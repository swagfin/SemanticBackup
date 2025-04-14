using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BackupBackgroundJob : IHostedService
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupBackgroundJob(ILogger<BackupBackgroundJob> logger, SystemConfigOptions persistanceOptions, IServiceScopeFactory serviceScopeFactory, BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            _logger = logger;
            _persistanceOptions = persistanceOptions;
            _serviceScopeFactory = serviceScopeFactory;
            _botsManagerBackgroundJob = botsManagerBackgroundJob;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService(cancellationToken);
            SetupBackgroundRemovedExpiredBackupsService(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void SetupBackgroundService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //Await
                    await Task.Delay(5000, cancellationToken);
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI Injections
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        IDatabaseInfoRepository databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                        IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                        //Proceed
                        List<BackupRecord> queuedBackups = await backupRecordPersistanceService.GetAllByStatusAsync(BackupRecordStatus.QUEUED.ToString());
                        if (queuedBackups != null && queuedBackups.Count > 0)
                        {
                            List<long> scheduleToDelete = [];
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
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupDatabaseInfo.ResourceGroupId);
                                    if (resourceGroup == null)
                                    {
                                        _logger.LogWarning($"The Database Id: {backupRecord.BackupDatabaseInfoId}, doesn't seem to have been assigned to a valid Resource Group Id: {backupDatabaseInfo.ResourceGroupId}, Record will be Deleted");
                                        scheduleToDelete.Add(backupRecord.Id);
                                    }
                                    else
                                    {
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                        {
                                            if (resourceGroup.DbType.Contains("SQLSERVER"))
                                                _botsManagerBackgroundJob.AddBot(new SQLBackupBot(backupDatabaseInfo.DatabaseName, resourceGroup, backupRecord, _serviceScopeFactory));
                                            else if (resourceGroup.DbType.Contains("MYSQL") || resourceGroup.DbType.Contains("MARIADB"))
                                                _botsManagerBackgroundJob.AddBot(new MySQLBackupBot(backupDatabaseInfo.DatabaseName, resourceGroup, backupRecord, _serviceScopeFactory));
                                            else
                                                throw new Exception($"No Bot is registered to Handle Database Backups of Type: {resourceGroup.DbType}");
                                            //Finally Update Status
                                            bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.EXECUTING.ToString());
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            });
            t.Start();
        }

        private void SetupBackgroundRemovedExpiredBackupsService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(3000); //Runs After 3sec
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI Injections
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
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
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                IContentDeliveryRecordRepository contentDeliveryRecordsService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                BotsManagerBackgroundJob botsManagerBackgroundJob = scope.ServiceProvider.GetRequiredService<BotsManagerBackgroundJob>();
                IDatabaseInfoRepository databaseInfoRepository = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                //get db information
                BackupDatabaseInfo backupRecordDbInfo = await databaseInfoRepository.GetByIdAsync(rm.BackupDatabaseInfoId);
                //Check if valid Resource Group
                ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                if (resourceGroup == null)
                    return;
                //Proceed
                List<BackupRecordDelivery> dbRecords = await contentDeliveryRecordsService.GetAllByBackupRecordIdAsync(rm.Id); //database record content delivery
                if (dbRecords == null)
                    return;
                List<string> supportedInDepthDelete = [BackupDeliveryConfigTypes.Dropbox.ToString(), BackupDeliveryConfigTypes.AzureBlobStorage.ToString()];
                List<BackupRecordDelivery> supportedDeliveryRecords = [.. dbRecords.Where(x => supportedInDepthDelete.Contains(x.DeliveryType))];
                if (supportedDeliveryRecords == null || supportedDeliveryRecords.Count == 0)
                    return;
                foreach (BackupRecordDelivery rec in supportedDeliveryRecords)
                {
                    if (rec.DeliveryType == BackupDeliveryConfigTypes.Dropbox.ToString())
                    {
                        //In Depth Remove From DropBox
                        botsManagerBackgroundJob.AddBot(new InDepthDeleteDropboxBot(resourceGroup, rm, rec, _serviceScopeFactory));
                    }
                    else if (rec.DeliveryType == BackupDeliveryConfigTypes.ObjectStorage.ToString())
                    {
                        //In Depth Remove From Object Storage
                        botsManagerBackgroundJob.AddBot(new InDepthDeleteObjectStorageBot(resourceGroup, rm, rec, _serviceScopeFactory));
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
        }
    }
}
