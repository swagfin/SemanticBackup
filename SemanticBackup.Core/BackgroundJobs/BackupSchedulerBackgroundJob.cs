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
        private readonly IContentDeliveryRecordPersistanceService _contentDeliveryRecordPersistanceService;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupSchedulerBackgroundJob(ILogger<BackupSchedulerBackgroundJob> logger,
            PersistanceOptions persistanceOptions,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            IResourceGroupPersistanceService resourceGroupPersistanceService,
            IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService, BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
            this._botsManagerBackgroundJob = botsManagerBackgroundJob;
        }
        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
            SetupBackgroundNonResponsiveStopService();
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

                }
            });
            t.Start();
        }

        private void SetupBackgroundNonResponsiveStopService()
        {
            var t = new Thread(async () =>
            {
                List<string> statusChecks = new List<string> { BackupRecordBackupStatus.EXECUTING.ToString(), BackupRecordBackupStatus.COMPRESSING.ToString(), ContentDeliveryRecordStatus.EXECUTING.ToString() };
                int executionTimeoutInMinutes = _persistanceOptions.ExecutionTimeoutInMinutes < 1 ? 1 : _persistanceOptions.ExecutionTimeoutInMinutes;
                while (true)
                {
                    try
                    {
                        List<string> botsToRemove = new List<string>();
                        //REMOVE BACKUP RECORDS
                        List<string> recordsIds = this._backupRecordPersistanceService.GetAllNoneResponsiveIds(statusChecks, executionTimeoutInMinutes);
                        if (recordsIds != null && recordsIds.Count > 0)
                            foreach (string id in recordsIds)
                            {
                                this._backupRecordPersistanceService.UpdateStatusFeed(id, BackupRecordBackupStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                                botsToRemove.Add(id);
                            }

                        //REMOVE CONTENT DELIVERY RECORDS
                        List<string> deliveryRecordIds = this._contentDeliveryRecordPersistanceService.GetAllNoneResponsive(statusChecks, executionTimeoutInMinutes);
                        if (deliveryRecordIds != null && deliveryRecordIds.Count > 0)
                            foreach (string id in deliveryRecordIds)
                            {
                                this._contentDeliveryRecordPersistanceService.UpdateStatusFeed(id, BackupRecordBackupStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                                botsToRemove.Add(id);
                            }

                        //Finally Try And Stop
                        if (botsToRemove.Count > 0)
                            _botsManagerBackgroundJob.TerminateBots(botsToRemove);
                    }
                    catch (Exception ex) { _logger.LogWarning($"Stopping Non Responsive Services Error: {ex.Message}"); }
                    //Delay
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            });
            t.Start();
        }
    }
}
