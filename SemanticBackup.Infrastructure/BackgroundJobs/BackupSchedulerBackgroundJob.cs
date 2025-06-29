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

        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _deliveryRecordRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        public BackupSchedulerBackgroundJob(
            ILogger<BackupSchedulerBackgroundJob> logger,
            SystemConfigOptions persistanceOptions,
            BotsManagerBackgroundJob botsManagerBackgroundJob,

            IResourceGroupRepository resourceGroupRepository,
            IBackupScheduleRepository backupScheduleRepository,
            IBackupRecordRepository backupRecordRepository,
            IContentDeliveryRecordRepository contentDeliveryRecordRepository,
            IDatabaseInfoRepository databaseInfoRepository
            )
        {
            _logger = logger;
            _persistanceOptions = persistanceOptions;
            _botsManagerBackgroundJob = botsManagerBackgroundJob;

            _resourceGroupRepository = resourceGroupRepository;
            _backupScheduleRepository = backupScheduleRepository;
            _backupRecordRepository = backupRecordRepository;
            _deliveryRecordRepository = contentDeliveryRecordRepository;
            _databaseInfoRepository = databaseInfoRepository;
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
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    try
                    {
                        //Proceed
                        DateTime currentTimeUTC = DateTime.UtcNow;
                        List<BackupSchedule> dueSchedules = (await _backupScheduleRepository.GetAllDueByDateAsync()) ?? [];
                        //proceed
                        List<string> scheduleToDelete = [];
                        foreach (BackupSchedule schedule in dueSchedules.OrderBy(x => x.Id).ToList())
                        {
                            BackupDatabaseInfo backupDatabaseInfo = await _databaseInfoRepository.GetByIdAsync(schedule.BackupDatabaseInfoId);
                            if (backupDatabaseInfo == null)
                            {
                                _logger.LogWarning("No Database Info matches with Id: {BackupDatabaseInfoId}, Schedule Record will be Deleted: {Id}", schedule.BackupDatabaseInfoId, schedule.Id);
                                scheduleToDelete.Add(schedule.Id);
                            }
                            else
                            {
                                //Proceed
                                ResourceGroup resourceGroup = await _resourceGroupRepository.GetByIdOrKeyAsync(backupDatabaseInfo.ResourceGroupId);
                                if (resourceGroup == null)
                                {
                                    _logger.LogWarning("Can NOT queue Database for Backup Id: {Id}, Reason: Assigned Resource Group doen't exist, Resource Group Id: {ResourceGroupId}, Schedule will be Removed", backupDatabaseInfo.Id, backupDatabaseInfo.ResourceGroupId);
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

                                    //schedule run
                                    _ = await _backupRecordRepository.AddOrUpdateAsync(newRecord);
                                    //set last run 
                                    _ = await _backupScheduleRepository.UpdateLastRunAsync(schedule.Id, currentTimeUTC);
                                }
                            }

                        }
                        //Check if Any Delete
                        foreach (string scheduleId in scheduleToDelete)
                            await _backupScheduleRepository.RemoveAsync(scheduleId);
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
                List<string> statusChecks = [BackupRecordStatus.EXECUTING.ToString(), BackupRecordStatus.COMPRESSING.ToString(), BackupRecordDeliveryStatus.EXECUTING.ToString()];
                int executionTimeoutInMinutes = _persistanceOptions.ExecutionTimeoutInMinutes < 1 ? 1 : _persistanceOptions.ExecutionTimeoutInMinutes;
                while (!cancellationToken.IsCancellationRequested)
                {
                    //Delay
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    try
                    {
                        //Proceed
                        List<string> botsToRemove = [];
                        //REMOVE BACKUP RECORDS
                        List<long> recordsIds = (await _backupRecordRepository.GetAllNoneResponsiveIdsAsync(statusChecks, executionTimeoutInMinutes)) ?? [];
                        foreach (long id in recordsIds)
                        {
                            await _backupRecordRepository.UpdateStatusFeedAsync(id, BackupRecordStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                            botsToRemove.Add(id.ToString());
                        }

                        //REMOVE CONTENT DELIVERY RECORDS
                        List<string> deliveryRecordIds = (await _deliveryRecordRepository.GetAllNoneResponsiveAsync(statusChecks, executionTimeoutInMinutes)) ?? [];
                        foreach (string id in deliveryRecordIds)
                        {
                            await _deliveryRecordRepository.UpdateStatusFeedAsync(id, BackupRecordStatus.ERROR.ToString(), "Bot Execution Timeout", executionTimeoutInMinutes);
                            botsToRemove.Add(id);
                        }

                        //Finally Try And Stop
                        if (botsToRemove.Count > 0)
                            _botsManagerBackgroundJob.TerminateBots(botsToRemove);
                    }
                    catch (Exception ex) { _logger.LogWarning($"Stopping Non Responsive Services Error: {ex.Message}"); }

                }
            });
            t.Start();
        }
    }
}
