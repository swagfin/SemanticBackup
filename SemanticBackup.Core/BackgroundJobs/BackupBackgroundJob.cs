using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly ISQLServerBackupProviderService _sQLServerBackupProviderService;
        private readonly IMySQLServerBackupProviderService _mySQLServerBackupProviderService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupBackgroundJob(ILogger<BackupBackgroundJob> logger,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            ISQLServerBackupProviderService sQLServerBackupProviderService,
            IMySQLServerBackupProviderService mySQLServerBackupProviderService, IResourceGroupPersistanceService resourceGroupPersistanceService, BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._sQLServerBackupProviderService = sQLServerBackupProviderService;
            this._mySQLServerBackupProviderService = mySQLServerBackupProviderService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
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
                    try
                    {
                        List<BackupRecord> queuedBackups = this._backupRecordPersistanceService.GetAllByStatus(BackupRecordBackupStatus.QUEUED.ToString());
                        if (queuedBackups != null && queuedBackups.Count > 0)
                        {
                            List<string> scheduleToDelete = new List<string>();
                            foreach (BackupRecord backupRecord in queuedBackups)
                            {
                                _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...");
                                BackupDatabaseInfo backupDatabaseInfo = this._databaseInfoPersistanceService.GetById(backupRecord.BackupDatabaseInfoId);
                                if (backupDatabaseInfo == null)
                                {
                                    _logger.LogWarning($"No Database Info matches with Id: {backupRecord.BackupDatabaseInfoId}, Backup Database Record will be Deleted: {backupRecord.Id}");
                                    scheduleToDelete.Add(backupRecord.Id);
                                }
                                else
                                {
                                    //Check if valid Resource Group
                                    ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupDatabaseInfo.ResourceGroupId);
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
                                                _botsManagerBackgroundJob.AddBot(new SQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, this._sQLServerBackupProviderService, _backupRecordPersistanceService, _logger));
                                            else if (backupDatabaseInfo.DatabaseType.Contains("MYSQL") || backupDatabaseInfo.DatabaseType.Contains("MARIADB"))
                                                _botsManagerBackgroundJob.AddBot(new MySQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, this._mySQLServerBackupProviderService, _backupRecordPersistanceService, _logger));
                                            else
                                                throw new Exception($"No Bot is registered to Handle Database Backups of Type: {backupDatabaseInfo.DatabaseType}");
                                            //Finally Update Status
                                            bool updated = this._backupRecordPersistanceService.UpdateStatusFeed(backupRecord.Id, BackupRecordBackupStatus.EXECUTING.ToString());
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
                                    this._backupRecordPersistanceService.Remove(rm);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    //Await
                    await Task.Delay(10000);
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
                        List<BackupRecord> expiredBackups = this._backupRecordPersistanceService.GetAllExpired();
                        if (expiredBackups != null && expiredBackups.Count > 0)
                        {
                            List<string> toDeleteList = new List<string>();
                            foreach (BackupRecord backupRecord in expiredBackups)
                                toDeleteList.Add(backupRecord.Id);
                            _logger.LogInformation($"Queued ({expiredBackups.Count}) Expired Records for Delete");
                            //Check if Any Delete
                            if (toDeleteList.Count > 0)
                                foreach (var rm in toDeleteList)
                                    if (!this._backupRecordPersistanceService.Remove(rm))
                                        _logger.LogWarning("Unable to delete Expired Backup Record");
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
