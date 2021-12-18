using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupBackgroundJob(
            ILogger<BackupBackgroundJob> logger,
            IServiceScopeFactory serviceScopeFactory,
            BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
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
                    await Task.Delay(10000);
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
                                foreach (BackupRecord backupRecord in queuedBackups)
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
                    await Task.Delay(60000); //Runs After 1 Minute
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
                                List<string> toDeleteList = new List<string>();
                                foreach (BackupRecord backupRecord in expiredBackups)
                                    toDeleteList.Add(backupRecord.Id);
                                _logger.LogInformation($"Queued ({expiredBackups.Count}) Expired Records for Delete");
                                //Check if Any Delete
                                if (toDeleteList.Count > 0)
                                    foreach (var rm in toDeleteList)
                                        if (!(await backupRecordPersistanceService.RemoveAsync(rm)))
                                            _logger.LogWarning("Unable to delete Expired Backup Record");
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
    }
}
