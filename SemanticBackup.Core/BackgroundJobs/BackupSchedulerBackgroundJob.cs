using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupSchedulerBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<BackupSchedulerBackgroundJob> _logger;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;

        public BackupSchedulerBackgroundJob(ILogger<BackupSchedulerBackgroundJob> logger,
            PersistanceOptions persistanceOptions,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
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
                        DateTime currentTimeUTC = DateTime.UtcNow;
                        List<BackupSchedule> dueSchedules = this._backupSchedulePersistanceService.GetAllDueByDate();
                        if (dueSchedules != null && dueSchedules.Count > 0)
                        {
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
                                else
                                {
                                    //Proceed
                                    ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupDatabaseInfo.ResourceGroupId);
                                    if (resourceGroup == null)
                                    {
                                        _logger.LogWarning($"Can NOT queue Database for Backup Id: {backupDatabaseInfo.Id}, Reason: Assigned Resource Group doen't exist, Resource Group Id: {backupDatabaseInfo.Id}, Schedule will be Removed");
                                        scheduleToDelete.Add(schedule.Id);
                                    }
                                    else
                                    {
                                        //has valid Resource Group Proceed
                                        DateTime resourceGroupLocalTime = DateTime.UtcNow.ConvertFromUTC(resourceGroup?.TimeZone);
                                        DateTime RecordExpiryUTC = currentTimeUTC.AddDays(resourceGroup.BackupExpiryAgeInDays);
                                        BackupRecord newRecord = new BackupRecord
                                        {
                                            BackupDatabaseInfoId = schedule.BackupDatabaseInfoId,
                                            ResourceGroupId = backupDatabaseInfo.ResourceGroupId,
                                            BackupStatus = BackupRecordBackupStatus.QUEUED.ToString(),
                                            ExpiryDateUTC = RecordExpiryUTC,
                                            Name = backupDatabaseInfo.Name,
                                            Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, SharedFunctions.GetSavingPathFromFormat(backupDatabaseInfo, _persistanceOptions.BackupFileSaveFormat, resourceGroupLocalTime)),
                                            StatusUpdateDateUTC = currentTimeUTC,
                                            RegisteredDateUTC = currentTimeUTC,
                                            ExecutedDeliveryRun = false
                                        };

                                        bool addedSuccess = this._backupRecordPersistanceService.AddOrUpdate(newRecord);
                                        if (!addedSuccess)
                                            throw new Exception("Unable to Queue Database for Backup");
                                        else
                                            _logger.LogInformation($"Queueing Scheduled Backup...SUCCESS");
                                        //Update Schedule
                                        schedule.LastRunUTC = currentTimeUTC;
                                        bool updatedSchedule = this._backupSchedulePersistanceService.Update(schedule);
                                        if (!updatedSchedule)
                                            _logger.LogWarning("Unable to Update Scheduled Next Run");
                                    }

                                }

                            }
                            //Check if Any Delete
                            if (scheduleToDelete.Count > 0)
                                foreach (var rm in scheduleToDelete)
                                    this._backupSchedulePersistanceService.Remove(rm);
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
