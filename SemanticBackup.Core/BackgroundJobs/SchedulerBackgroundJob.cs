using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class SchedulerBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<SchedulerBackgroundJob> _logger;
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;

        public SchedulerBackgroundJob(ILogger<SchedulerBackgroundJob> logger, SharedTimeZone sharedTimeZone, IBackupSchedulePersistanceService backupSchedulePersistanceService)
        {
            this._logger = logger;
            this._sharedTimeZone = sharedTimeZone;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
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
                        var dueSchedules = this._backupSchedulePersistanceService.GetAllDueByDate(currentTime);
                        if (dueSchedules == null)
                            return;
                        foreach (var schedule in dueSchedules)
                        {
                            _logger.LogInformation($"SCHEDULE DUE: {schedule.Type}, ID: {schedule.Id},  FOR DB: {schedule.BackupDatabaseInfoId}");
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
    }
}
