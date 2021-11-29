using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly List<IBackupRobot> BackupsBots;

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
            this.BackupsBots = new List<IBackupRobot>();
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
                                    BackupsBots.Add(new SQLBackupRobot(backupDatabaseInfo, backupRecord, this._sQLServerBackupProviderService, _backupRecordPersistanceService, _sharedTimeZone, _logger));
                                else
                                    throw new Exception($"No Bot is registered to Handle Database Backups of Type: {backupDatabaseInfo.DatabaseType}");
                                //Finally Update Status
                                bool updated = this._backupRecordPersistanceService.UpdateStatusFeed(backupRecord.Id, BackupRecordBackupStatus.EXECUTING.ToString(), currentTime);
                                if (updated)
                                    _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...SUCCESS");
                                else
                                    _logger.LogWarning("Unable to Update Queued Backup Status");
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
                            int runningThreads = this.BackupsBots.Count(x => x.IsStarted && !x.IsCompleted);
                            int takeCount = _persistanceOptions.MaximumBackupRunningThreads - runningThreads;
                            if (takeCount > 0)
                            {
                                List<IBackupRobot> botsNotStarted = this.BackupsBots.Where(x => !x.IsStarted).Take(takeCount).ToList();
                                if (botsNotStarted != null && botsNotStarted.Count > 0)
                                    foreach (IBackupRobot bot in botsNotStarted)
                                        _ = bot.RunAsync();
                            }
                            //Remove Completed
                            List<IBackupRobot> botsCompleted = this.BackupsBots.Where(x => x.IsCompleted).ToList();
                            if (botsCompleted != null && botsCompleted.Count > 0)
                                foreach (IBackupRobot bot in botsCompleted)
                                    this.BackupsBots.Remove(bot);
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning($"Running Unstarted and Removing Completed Bots Failed: {ex.Message}"); }
                    //Delay
                    await Task.Delay(2000);
                }
            });
            t.Start();
        }
    }

    public interface IBackupRobot
    {
        Task RunAsync();
        bool IsCompleted { get; }
        bool IsStarted { get; }
    }
    public class SQLBackupRobot : IBackupRobot
    {
        private readonly BackupDatabaseInfo _databaseInfo;
        private readonly BackupRecord _backupRecord;
        private readonly ISQLServerBackupProviderService _backupProviderService;
        private readonly IBackupRecordPersistanceService _persistanceService;
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public SQLBackupRobot(BackupDatabaseInfo databaseInfo, BackupRecord backupRecord, ISQLServerBackupProviderService backupProviderService, IBackupRecordPersistanceService persistanceService, SharedTimeZone sharedTimeZone, ILogger logger)
        {
            this._databaseInfo = databaseInfo;
            this._backupRecord = backupRecord;
            this._backupProviderService = backupProviderService;
            this._persistanceService = persistanceService;
            this._sharedTimeZone = sharedTimeZone;
            this._logger = logger;
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Creating Backup of Db: {_databaseInfo.DatabaseName}");
                EnsureFolderExists(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();
                //Execute Service
                bool backupedUp = _backupProviderService.BackupDatabase(_databaseInfo, _backupRecord);
                stopwatch.Stop();
                if (backupedUp)
                    UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.COMPLETED.ToString(), "Successfull", stopwatch.ElapsedMilliseconds);
                else
                    throw new Exception("Creating Backup Failed to Return Success Completion");
                _logger.LogInformation($"Creating Backup of Db: {_databaseInfo.DatabaseName}...SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private void EnsureFolderExists(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {
                DateTime currentTime = _sharedTimeZone.Now;
                _persistanceService.UpdateStatusFeed(recordId, status, currentTime, message, elapsed);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error Updating Feed: " + ex.Message);
            }
            finally
            {
                IsCompleted = true;
            }
        }
    }
}
