using Microsoft.Extensions.DependencyInjection;
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
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BackupSchedulerBackgroundJob(
            ILogger<BackupSchedulerBackgroundJob> logger,
            PersistanceOptions persistanceOptions,
            IServiceScopeFactory serviceScopeFactory,
            BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._serviceScopeFactory = serviceScopeFactory;
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
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupSchedulePersistanceService backupSchedulePersistanceService = scope.ServiceProvider.GetRequiredService<IBackupSchedulePersistanceService>();
                            IDatabaseInfoPersistanceService databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoPersistanceService>();
                            IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            //Proceed
                            DateTime currentTimeUTC = DateTime.UtcNow;
                            List<BackupSchedule> dueSchedules = await backupSchedulePersistanceService.GetAllDueByDateAsync();
                            if (dueSchedules != null && dueSchedules.Count > 0)
                            {
                                List<string> scheduleToDelete = new List<string>();
                                foreach (BackupSchedule schedule in dueSchedules)
                                {
                                    _logger.LogInformation($"Queueing Scheduled Backup...");
                                    BackupDatabaseInfo backupDatabaseInfo = await databaseInfoPersistanceService.GetByIdAsync(schedule.BackupDatabaseInfoId);
                                    if (backupDatabaseInfo == null)
                                    {
                                        _logger.LogWarning($"No Database Info matches with Id: {schedule.BackupDatabaseInfoId}, Schedule Record will be Deleted: {schedule.Id}");
                                        scheduleToDelete.Add(schedule.Id);
                                    }
                                    else
                                    {
                                        //Proceed
                                        ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo.ResourceGroupId);
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

                                            bool addedSuccess = await backupRecordPersistanceService.AddOrUpdateAsync(newRecord);
                                            if (!addedSuccess)
                                                throw new Exception("Unable to Queue Database for Backup");
                                            else
                                                _logger.LogInformation($"Queueing Scheduled Backup...SUCCESS");
                                            //Update Schedule
                                            schedule.LastRunUTC = currentTimeUTC;
                                            bool updatedSchedule = await backupSchedulePersistanceService.UpdateAsync(schedule);
                                            if (!updatedSchedule)
                                                _logger.LogWarning("Unable to Update Scheduled Next Run");
                                        }

                                    }

                                }
                                //Check if Any Delete
                                if (scheduleToDelete.Count > 0)
                                    foreach (var rm in scheduleToDelete)
                                        await backupSchedulePersistanceService.RemoveAsync(rm);
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
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordPersistanceService>();
                            //Proceed
                            List<string> botsToRemove = new List<string>();
                            //REMOVE BACKUP RECORDS
                            List<string> recordsIds = await backupRecordPersistanceService.GetAllNoneResponsiveIdsAsync(statusChecks, executionTimeoutInMinutes);
                            if (recordsIds != null && recordsIds.Count > 0)
                                foreach (string id in recordsIds)
                                {
                                    await backupRecordPersistanceService.UpdateStatusFeedAsync(id, BackupRecordBackupStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                                    botsToRemove.Add(id);
                                }

                            //REMOVE CONTENT DELIVERY RECORDS
                            List<string> deliveryRecordIds = await contentDeliveryRecordPersistanceService.GetAllNoneResponsiveAsync(statusChecks, executionTimeoutInMinutes);
                            if (deliveryRecordIds != null && deliveryRecordIds.Count > 0)
                                foreach (string id in deliveryRecordIds)
                                {
                                    await contentDeliveryRecordPersistanceService.UpdateStatusFeedAsync(id, BackupRecordBackupStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                                    botsToRemove.Add(id);
                                }

                            //Finally Try And Stop
                            if (botsToRemove.Count > 0)
                                _botsManagerBackgroundJob.TerminateBots(botsToRemove);
                        }
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
