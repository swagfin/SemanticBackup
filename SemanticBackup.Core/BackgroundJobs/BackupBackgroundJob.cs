using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupBackgroundJob(
            ILogger<BackupBackgroundJob> logger,
            PersistanceOptions persistanceOptions,
            IServiceScopeFactory serviceScopeFactory,
            BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._serviceScopeFactory = serviceScopeFactory;
            this._botsManagerBackgroundJob = botsManagerBackgroundJob;
        }
        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
            SetupBackgroundRemovedExpiredBackupsService();
            _logger.LogInformation("Service Started");
        }

        private void SetupBackgroundService()
        {
            var t = new Thread(async () =>
            {
                while (true)
                {
                    //Await
                    await Task.Delay(5000);
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI Injections
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            IDatabaseInfoPersistanceService databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoPersistanceService>();
                            IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                            //Proceed
                            List<BackupRecord> queuedBackups = await backupRecordPersistanceService.GetAllByStatusAsync(BackupRecordBackupStatus.QUEUED.ToString());
                            if (queuedBackups != null && queuedBackups.Count > 0)
                            {
                                List<string> scheduleToDelete = new List<string>();
                                foreach (BackupRecord backupRecord in queuedBackups.OrderBy(x => x.RegisteredDateUTC).ToList())
                                {
                                    _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...");
                                    BackupDatabaseInfo backupDatabaseInfo = await databaseInfoPersistanceService.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                    if (backupDatabaseInfo == null)
                                    {
                                        _logger.LogWarning($"No Database Info matches with Id: {backupRecord.BackupDatabaseInfoId}, Backup Database Record will be Deleted: {backupRecord.Id}");
                                        scheduleToDelete.Add(backupRecord.Id);
                                    }
                                    else
                                    {
                                        //Check if valid Resource Group
                                        ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo.ResourceGroupId);
                                        if (resourceGroup == null)
                                        {
                                            _logger.LogWarning($"The Database Id: {backupRecord.BackupDatabaseInfoId}, doesn't seem to have been assigned to a valid Resource Group Id: {backupDatabaseInfo.ResourceGroupId}, Record will be Deleted");
                                            scheduleToDelete.Add(backupRecord.Id);
                                        }
                                        else
                                        {
                                            if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                            {
                                                if (backupDatabaseInfo.DatabaseType.Contains("SQLSERVER"))
                                                    _botsManagerBackgroundJob.AddBot(new SQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, _serviceScopeFactory));
                                                else if (backupDatabaseInfo.DatabaseType.Contains("MYSQL") || backupDatabaseInfo.DatabaseType.Contains("MARIADB"))
                                                    _botsManagerBackgroundJob.AddBot(new MySQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, _serviceScopeFactory));
                                                else
                                                    throw new Exception($"No Bot is registered to Handle Database Backups of Type: {backupDatabaseInfo.DatabaseType}");
                                                //Finally Update Status
                                                bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordBackupStatus.EXECUTING.ToString());
                                                if (updated)
                                                    _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...SUCCESS");
                                                else
                                                    _logger.LogWarning($"Queued for Backup but was unable to update backup record Key: #{backupRecord.Id} status");
                                            }
                                            else
                                                _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} has Exceeded its Maximum Allocated Running Threads Count: {resourceGroup.MaximumRunningBots}");
                                        }

                                    }
                                }
                                //Check if Any Delete
                                if (scheduleToDelete.Count > 0)
                                    foreach (var rm in scheduleToDelete)
                                        await backupRecordPersistanceService.RemoveAsync(rm);
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }

                }
            });
            t.Start();
        }

        private void SetupBackgroundRemovedExpiredBackupsService()
        {
            var t = new Thread(async () =>
            {
                while (true)
                {
#if DEBUG
                    await Task.Delay(3000); //Runs After 3sec
#else
                    await Task.Delay(60000); //Runs After 1 Minute
#endif
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI Injections
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            //Proceed
                            List<BackupRecord> expiredBackups = await backupRecordPersistanceService.GetAllExpiredAsync();
                            if (expiredBackups != null && expiredBackups.Count > 0)
                            {
                                foreach (BackupRecord rm in expiredBackups.Take(50).ToList())
                                    if (!await backupRecordPersistanceService.RemoveAsync(rm.Id))
                                        _logger.LogWarning($"Unable to delete Expired Backup Record: {rm.Id}");
                                    else
                                    {
                                        _logger.LogInformation($"Removed Expired Backup Record, Id: {rm.Id}");
                                        //Options InDepth Delete
                                        if (_persistanceOptions.InDepthBackupRecordDeleteEnabled)
                                            await StartInDepthDeleteForAsync(rm);
                                    }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            });
            t.Start();
        }

        private async Task StartInDepthDeleteForAsync(BackupRecord rm)
        {
            try
            {
                if (rm == null) return;
                //scope
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                    IContentDeliveryRecordPersistanceService contentDeliveryRecordsService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordPersistanceService>();
                    IContentDeliveryConfigPersistanceService contentDeliveryConfigPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryConfigPersistanceService>();
                    BotsManagerBackgroundJob botsManagerBackgroundJob = scope.ServiceProvider.GetRequiredService<BotsManagerBackgroundJob>();
                    ResourceGroup backRecordResourceGrp = await resourceGroupPersistanceService.GetByIdAsync(rm.ResourceGroupId);
                    if (backRecordResourceGrp == null)
                    {
                        _logger.LogWarning($"InDepth Deletion Failed, the Database Record has no valid Resource Group, Id: {rm.ResourceGroupId}, resource Group: {rm.ResourceGroupId}");
                        return;
                    }
                    //Proceed
                    var dbRecords = await contentDeliveryRecordsService.GetAllByBackupRecordIdAsync(rm.Id); //database record content delivery
                    if (dbRecords == null)
                        return;
                    List<string> supportedInDepthDelete = new List<string> { ContentDeliveryType.DROPBOX.ToString(), ContentDeliveryType.AZURE_BLOB_STORAGE.ToString() };
                    List<ContentDeliveryRecord> supportedDeliveryRecords = dbRecords.Where(x => supportedInDepthDelete.Contains(x.DeliveryType)).ToList();
                    if (supportedDeliveryRecords == null || supportedDeliveryRecords.Count == 0)
                        return;
                    foreach (var rec in supportedDeliveryRecords)
                    {
                        ContentDeliveryConfiguration config = await contentDeliveryConfigPersistanceService.GetByIdAsync(rec.ContentDeliveryConfigurationId);
                        if (rec.DeliveryType == ContentDeliveryType.DROPBOX.ToString())
                        {
                            //In Depth Remove From DropBox
                            botsManagerBackgroundJob.AddBot(new InDepthDeleteDropboxBot(rm, rec, config, _serviceScopeFactory));
                        }
                        else if (rec.DeliveryType == ContentDeliveryType.AZURE_BLOB_STORAGE.ToString())
                        {
                            //In Depth remove From Azure Storage
                            botsManagerBackgroundJob.AddBot(new InDepthDeleteAzureStorageBot(rm, rec, config, _serviceScopeFactory));
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
        }
    }
}
