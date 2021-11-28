using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class SchedulerBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<SchedulerBackgroundJob> _logger;
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;

        public SchedulerBackgroundJob(ILogger<SchedulerBackgroundJob> logger,
            SharedTimeZone sharedTimeZone,
            PersistanceOptions persistanceOptions,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService)
        {
            this._logger = logger;
            this._sharedTimeZone = sharedTimeZone;
            this._persistanceOptions = persistanceOptions;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }
        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
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
                        List<BackupSchedule> dueSchedules = this._backupSchedulePersistanceService.GetAllDueByDate(currentTime);
                        if (dueSchedules == null)
                            return;
                        List<string> scheduleToDelete = new List<string>();
                        foreach (BackupSchedule schedule in dueSchedules)
                        {
                            _logger.LogInformation($"Queueing Scheduled Backup...");
                            BackupDatabaseInfo backupDatabaseInfo = this._databaseInfoPersistanceService.GetById(schedule.BackupDatabaseInfoId);
                            if (backupDatabaseInfo == null)
                            {
                                _logger.LogWarning($"No Database Info matches with Id: {schedule.BackupDatabaseInfoId}, Schedule Record will be Deleted: {schedule.Id}");
                                scheduleToDelete.Add(schedule.Id);
                            }
                            //Proceed
                            BackupRecord newRecord = new BackupRecord
                            {
                                BackupDatabaseInfoId = schedule.BackupDatabaseInfoId,
                                BackupStatus = BackupRecordBackupStatus.QUEUED.ToString(),
                                ExpiryDate = null,
                                Name = backupDatabaseInfo.Name,
                                Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, backupDatabaseInfo.DatabaseName, $"{_sharedTimeZone.Now:yyyy-MM-dd}", $"{backupDatabaseInfo.DatabaseName.ToUpper()}-{_sharedTimeZone.Now:yyyy-MM-dd-mm-ss}.{backupDatabaseInfo.DatabaseType.ToLower()}.bak"),
                                StatusUpdateDate = _sharedTimeZone.Now
                            };

                            bool addedSuccess = this._backupRecordPersistanceService.AddOrUpdate(newRecord);
                            if (!addedSuccess)
                                throw new Exception("Unable to Queue Database for Backup");
                            else
                                _logger.LogInformation($"Queueing Scheduled Backup...SUCCESS");
                            //Update Schedule
                            schedule.LastRun = _sharedTimeZone.Now;
                            bool updatedSchedule = this._backupSchedulePersistanceService.Update(schedule);
                            if (!updatedSchedule)
                                _logger.LogWarning("Unable to Update Scheduled Next Run");
                        }
                        //Check if Any Delete
                        if (scheduleToDelete.Count > 0)
                            foreach (var rm in scheduleToDelete)
                                this._backupSchedulePersistanceService.Remove(rm);
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
    }
}
