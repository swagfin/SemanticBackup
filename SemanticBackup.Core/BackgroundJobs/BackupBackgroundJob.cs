using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
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
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly ISQLServerBackupProviderService _sQLServerBackupProviderService;
        internal readonly List<IBot> BackupsBots;

        public BackupBackgroundJob(ILogger<BackupBackgroundJob> logger,
            SharedTimeZone sharedTimeZone,
            PersistanceOptions persistanceOptions,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            ISQLServerBackupProviderService sQLServerBackupProviderService
            )
        {
            this._logger = logger;
            this._sharedTimeZone = sharedTimeZone;
            this._persistanceOptions = persistanceOptions;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._sQLServerBackupProviderService = sQLServerBackupProviderService;
            this.BackupsBots = new List<IBot>();
        }
        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
            SetupBotsBackgroundService();
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
                        DateTime currentTime = _sharedTimeZone.Now;
                        List<BackupRecord> queuedBackups = this._backupRecordPersistanceService.GetAllByStatus(BackupRecordBackupStatus.QUEUED.ToString());
                        if (queuedBackups == null)
                            return;
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
                                //Add Queue
                                if (backupDatabaseInfo.DatabaseType.Contains("SQLSERVER"))
                                    BackupsBots.Add(new SQLBackupBot(backupDatabaseInfo, backupRecord, this._sQLServerBackupProviderService, _backupRecordPersistanceService, _sharedTimeZone, _logger));
                                else
                                    throw new Exception($"No Bot is registered to Handle Database Backups of Type: {backupDatabaseInfo.DatabaseType}");
                                //Finally Update Status
                                bool updated = this._backupRecordPersistanceService.UpdateStatusFeed(backupRecord.Id, BackupRecordBackupStatus.EXECUTING.ToString(), currentTime);
                                if (updated)
                                    _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...SUCCESS");
                                else
                                    _logger.LogWarning($"Queued for Backup but was unable to update backup record Key: #{backupRecord.Id} status");
                            }

                        }
                        //Check if Any Delete
                        if (scheduleToDelete.Count > 0)
                            foreach (var rm in scheduleToDelete)
                                this._backupRecordPersistanceService.Remove(rm);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    //Await
                    await Task.Delay(30 * 1000);
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
                            int runningThreads = this.BackupsBots.Count(x => x.IsStarted && !x.IsCompleted);
                            int takeCount = _persistanceOptions.MaximumBackupRunningThreads - runningThreads;
                            if (takeCount > 0)
                            {
                                List<IBot> botsNotStarted = this.BackupsBots.Where(x => !x.IsStarted).Take(takeCount).ToList();
                                if (botsNotStarted != null && botsNotStarted.Count > 0)
                                    foreach (IBot bot in botsNotStarted)
                                        _ = bot.RunAsync();
                            }
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
    }
}
