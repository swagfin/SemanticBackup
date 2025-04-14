using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BackupSchedulerBackgroundJob : IHostedService
    {
        private readonly ILogger<BackupSchedulerBackgroundJob> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BackupSchedulerBackgroundJob(
            ILogger<BackupSchedulerBackgroundJob> logger,
            SystemConfigOptions persistanceOptions,
            IServiceScopeFactory serviceScopeFactory,
            BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._serviceScopeFactory = serviceScopeFactory;
            this._botsManagerBackgroundJob = botsManagerBackgroundJob;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService(cancellationToken);
            SetupBackgroundNonResponsiveStopService(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        private void SetupBackgroundService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(3000, cancellationToken);
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI INJECTIONS
                        IBackupScheduleRepository backupSchedulePersistanceService = scope.ServiceProvider.GetRequiredService<IBackupScheduleRepository>();
                        IDatabaseInfoRepository databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                        IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        //Proceed
                        DateTime currentTimeUTC = DateTime.UtcNow;
                        List<BackupSchedule> dueSchedules = await backupSchedulePersistanceService.GetAllDueByDateAsync();
                        if (dueSchedules != null && dueSchedules.Count > 0)
                        {
                            List<string> scheduleToDelete = new List<string>();
                            foreach (BackupSchedule schedule in dueSchedules.OrderBy(x => x.NextRunUTC).ToList())
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
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupDatabaseInfo.ResourceGroupId);
                                    if (resourceGroup == null)
                                    {
                                        _logger.LogWarning($"Can NOT queue Database for Backup Id: {backupDatabaseInfo.Id}, Reason: Assigned Resource Group doen't exist, Resource Group Id: {backupDatabaseInfo.Id}, Schedule will be Removed");
                                        scheduleToDelete.Add(schedule.Id);
                                    }
                                    else
                                    {
                                        //has valid Resource Group Proceed
                                        DateTime RecordExpiryUTC = currentTimeUTC.AddDays(resourceGroup.BackupExpiryAgeInDays);
                                        BackupRecord newRecord = new BackupRecord
                                        {
                                            BackupDatabaseInfoId = schedule.BackupDatabaseInfoId,
                                            BackupStatus = BackupRecordStatus.QUEUED.ToString(),
                                            ExpiryDateUTC = RecordExpiryUTC,
                                            Name = $"{backupDatabaseInfo.DatabaseName} on {resourceGroup.DbServer}",
                                            Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, resourceGroup.GetSavingPathFromFormat(backupDatabaseInfo.DatabaseName, _persistanceOptions.BackupFileSaveFormat, currentTimeUTC)),
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
                                        bool updatedSchedule = await backupSchedulePersistanceService.UpdateLastRunAsync(schedule.Id, currentTimeUTC);
                                        if (!updatedSchedule)
                                            _logger.LogWarning("Unable to Update Scheduled Next Run");
                                        //Buy Some Seconds to avoid Conflict Name
                                        await Task.Delay(new Random().Next(100));
                                    }

                                }

                            }
                            //Check if Any Delete
                            if (scheduleToDelete.Count > 0)
                                foreach (var rm in scheduleToDelete)
                                    await backupSchedulePersistanceService.RemoveAsync(rm);
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

        private void SetupBackgroundNonResponsiveStopService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                List<string> statusChecks = new List<string> { BackupRecordStatus.EXECUTING.ToString(), BackupRecordStatus.COMPRESSING.ToString(), BackupRecordDeliveryStatus.EXECUTING.ToString() };
                int executionTimeoutInMinutes = _persistanceOptions.ExecutionTimeoutInMinutes < 1 ? 1 : _persistanceOptions.ExecutionTimeoutInMinutes;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI INJECTIONS
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                        //Proceed
                        List<string> botsToRemove = [];
                        //REMOVE BACKUP RECORDS
                        List<long> recordsIds = await backupRecordPersistanceService.GetAllNoneResponsiveIdsAsync(statusChecks, executionTimeoutInMinutes);
                        if (recordsIds != null && recordsIds.Count > 0)
                            foreach (long id in recordsIds)
                            {
                                await backupRecordPersistanceService.UpdateStatusFeedAsync(id, BackupRecordStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                                botsToRemove.Add(id.ToString());
                            }

                        //REMOVE CONTENT DELIVERY RECORDS
                        List<string> deliveryRecordIds = await contentDeliveryRecordPersistanceService.GetAllNoneResponsiveAsync(statusChecks, executionTimeoutInMinutes);
                        if (deliveryRecordIds != null && deliveryRecordIds.Count > 0)
                            foreach (string id in deliveryRecordIds)
                            {
                                await contentDeliveryRecordPersistanceService.UpdateStatusFeedAsync(id, BackupRecordStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
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
