﻿using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
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
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly ISQLServerBackupProviderService _sQLServerBackupProviderService;
        private readonly IMySQLServerBackupProviderService _mySQLServerBackupProviderService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;
        internal readonly List<IBot> BackupsBots;

        public BackupBackgroundJob(ILogger<BackupBackgroundJob> logger,
            PersistanceOptions persistanceOptions,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            ISQLServerBackupProviderService sQLServerBackupProviderService,
            IMySQLServerBackupProviderService mySQLServerBackupProviderService, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._sQLServerBackupProviderService = sQLServerBackupProviderService;
            this._mySQLServerBackupProviderService = mySQLServerBackupProviderService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this.BackupsBots = new List<IBot>();
        }
        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
            SetupBotsBackgroundService();
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
                        if (queuedBackups != null)
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
                                        //Each Resource Group May have different Bots Running Time
                                        int runningResourceGrpThreads = this.BackupsBots.Where(x => x.resourceGroupId == resourceGroup.Id).Count(x => x.IsStarted && !x.IsCompleted);
                                        int availableResourceGrpThreads = resourceGroup.MaximumBackupRunningThreads - runningResourceGrpThreads;
                                        if (availableResourceGrpThreads > 0)
                                        {
                                            if (backupDatabaseInfo.DatabaseType.Contains("SQLSERVER"))
                                                BackupsBots.Add(new SQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, this._sQLServerBackupProviderService, _backupRecordPersistanceService, _logger));
                                            else if (backupDatabaseInfo.DatabaseType.Contains("MYSQL") || backupDatabaseInfo.DatabaseType.Contains("MARIADB"))
                                                BackupsBots.Add(new MySQLBackupBot(resourceGroup.Id, backupDatabaseInfo, backupRecord, this._mySQLServerBackupProviderService, _backupRecordPersistanceService, _logger));
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
                                            _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} has Exceeded its Maximum Allocated Running Threads Count: {resourceGroup.MaximumBackupRunningThreads}");
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

        private void SetupBotsBackgroundService()
        {
            var t = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (this.BackupsBots != null || this.BackupsBots.Count > 0)
                        {
                            //Start and Stop Bacup Bots
                            List<IBot> botsNotStarted = this.BackupsBots.Where(x => !x.IsStarted).ToList();
                            if (botsNotStarted != null && botsNotStarted.Count > 0)
                                foreach (IBot bot in botsNotStarted)
                                    _ = bot.RunAsync();
                            //Remove Completed
                            List<IBot> botsCompleted = this.BackupsBots.Where(x => x.IsCompleted).ToList();
                            if (botsCompleted != null && botsCompleted.Count > 0)
                                foreach (IBot bot in botsCompleted)
                                    this.BackupsBots.Remove(bot);
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning($"Running Unstarted and Removing Completed Bots Failed: {ex.Message}"); }
                    //Delay
                    await Task.Delay(5000);
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
                        if (expiredBackups != null || expiredBackups.Count > 0)
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
