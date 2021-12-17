using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Extensions;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class ContentDeliverySchedulerBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<ContentDeliverySchedulerBackgroundJob> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IContentDeliveryConfigPersistanceService _contentDeliveryConfigPersistanceService;
        private readonly IContentDeliveryRecordPersistanceService _contentDeliveryRecordPersistanceService;

        public ContentDeliverySchedulerBackgroundJob(ILogger<ContentDeliverySchedulerBackgroundJob> logger,
            IBackupRecordPersistanceService backupRecordPersistanceService,
            IContentDeliveryConfigPersistanceService contentDeliveryConfigPersistanceService,
            IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService)
        {
            this._logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._contentDeliveryConfigPersistanceService = contentDeliveryConfigPersistanceService;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
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
                        List<BackupRecord> pendingExecutionRecords = await this._backupRecordPersistanceService.GetAllReadyAndPendingDeliveryAsync();
                        if (pendingExecutionRecords != null && pendingExecutionRecords.Count > 0)
                        {
                            foreach (BackupRecord backupRecord in pendingExecutionRecords)
                            {
                                _logger.LogInformation($"Queueing Content Delivery for Backup Record Id: {backupRecord.Id}...");
                                //Has Valid Resource Group
                                List<ContentDeliveryConfiguration> resourceGroupContentDeliveryConfigs = await this._contentDeliveryConfigPersistanceService.GetAllAsync(backupRecord.ResourceGroupId);
                                if (resourceGroupContentDeliveryConfigs != null && resourceGroupContentDeliveryConfigs.Count > 0)
                                {
                                    List<string> scheduleToDelete = new List<string>();
                                    foreach (ContentDeliveryConfiguration config in resourceGroupContentDeliveryConfigs)
                                    {
                                        bool queuedSuccess = await this._contentDeliveryRecordPersistanceService.AddOrUpdateAsync(new ContentDeliveryRecord
                                        {
                                            Id = $"{backupRecord.Id}|{config.Id}|{config.ResourceGroupId}".ToMD5String().ToUpper(), //Unique Identification
                                            BackupRecordId = backupRecord.Id,
                                            ContentDeliveryConfigurationId = config.Id,
                                            ResourceGroupId = config.ResourceGroupId,
                                            CurrentStatus = ContentDeliveryRecordStatus.QUEUED.ToString(),
                                            DeliveryType = config.DeliveryType,
                                            RegisteredDateUTC = DateTime.UtcNow,
                                            StatusUpdateDateUTC = DateTime.UtcNow,
                                            ExecutionMessage = "Queued for Dispatch"
                                        });
                                        if (!queuedSuccess)
                                            _logger.LogWarning($"Unable to Queue Content Delivery Record of Type: {config.Id}, of Backup Record: {backupRecord.Id}");
                                    }
                                    //Update Execution
                                    bool savedSuccess = await this._backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.Id, true, BackupRecordExecutedDeliveryRunStatus.SUCCESSFULLY_EXECUTED.ToString());
                                    //Scheduled to Remove
                                    if (scheduleToDelete.Count > 0)
                                        foreach (var id in scheduleToDelete)
                                            await this._contentDeliveryConfigPersistanceService.RemoveAsync(id);
                                }
                                else
                                {
                                    _logger.LogWarning($"Resource Group Id: {backupRecord.Id}, doesn't have any content delivery config, Skipped Backup Record Content Delivery");
                                    await this._backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.ResourceGroupId, true, BackupRecordExecutedDeliveryRunStatus.SKIPPED_EXECUTION.ToString());
                                }

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }

                    //Await
                    await Task.Delay(3000);
                }
            });
            t.Start();
        }
    }
}
