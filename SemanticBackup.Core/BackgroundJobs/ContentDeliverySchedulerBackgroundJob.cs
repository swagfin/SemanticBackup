using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class ContentDeliverySchedulerBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<ContentDeliverySchedulerBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public ContentDeliverySchedulerBackgroundJob(
            ILogger<ContentDeliverySchedulerBackgroundJob> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this._logger = logger;
            this._serviceScopeFactory = serviceScopeFactory;
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
                    //Await
                    await Task.Delay(4000);
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                            IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                            IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                            IContentDeliveryConfigRepository contentDeliveryConfigPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryConfigRepository>();
                            IDatabaseInfoRepository databaseInfoRepository = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                            //Proceed
                            List<BackupRecord> pendingExecutionRecords = await backupRecordPersistanceService.GetAllReadyAndPendingDeliveryAsync();
                            if (pendingExecutionRecords != null && pendingExecutionRecords.Count > 0)
                            {
                                foreach (BackupRecord backupRecord in pendingExecutionRecords.OrderBy(x => x.RegisteredDateUTC).ToList())
                                {
                                    _logger.LogInformation($"Queueing Content Delivery for Backup Record Id: {backupRecord.Id}...");
                                    //get db information
                                    BackupDatabaseInfo backupRecordDbInfo = await databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                    //Check if valid Resource Group
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                                    //Has Valid Resource Group
                                    List<ContentDeliveryConfiguration> resourceGroupContentDeliveryConfigs = await contentDeliveryConfigPersistanceService.GetAllAsync(resourceGroup.Id ?? string.Empty);
                                    if (resourceGroupContentDeliveryConfigs != null && resourceGroupContentDeliveryConfigs.Count > 0)
                                    {
                                        List<string> scheduleToDelete = new List<string>();
                                        foreach (ContentDeliveryConfiguration config in resourceGroupContentDeliveryConfigs)
                                        {
                                            bool queuedSuccess = await contentDeliveryRecordPersistanceService.AddOrUpdateAsync(new BackupRecordDelivery
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
                                        bool savedSuccess = await backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.Id, true, BackupRecordExecutedDeliveryRunStatus.SUCCESSFULLY_EXECUTED.ToString());
                                        //Scheduled to Remove
                                        if (scheduleToDelete.Count > 0)
                                            foreach (var id in scheduleToDelete)
                                                await contentDeliveryConfigPersistanceService.RemoveAsync(id);
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Resource Group Id: {backupRecord.Id}, doesn't have any content delivery config, Skipped Backup Record Content Delivery");
                                        await backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.Id, true, BackupRecordExecutedDeliveryRunStatus.SKIPPED_EXECUTION.ToString());
                                    }

                                }

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
    }
}
