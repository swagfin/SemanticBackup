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
                            IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                            //Proceed
                            List<BackupRecord> pendingExecutionRecords = await backupRecordPersistanceService.GetAllReadyAndPendingDeliveryAsync();
                            foreach (BackupRecord backupRecord in pendingExecutionRecords?.OrderBy(x => x.RegisteredDateUTC)?.ToList())
                            {
                                _logger.LogInformation($"Queueing Content Delivery for Backup Record Id: {backupRecord.Id}...");
                                //## get other services
                                IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                                IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                                IDatabaseInfoRepository databaseInfoRepository = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                                //get db information
                                BackupDatabaseInfo backupRecordDbInfo = await databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                //Check if valid Resource Group
                                ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                                //Has Valid Resource Group

                                //check if backup delivery config is set
                                if (resourceGroup.BackupDeliveryConfig == null)
                                {
                                    _logger.LogInformation($"Resource Group Id: {backupRecord.Id}, doesn't have any backup delivery config, Skipped");
                                    _ = await backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.Id, true, BackupRecordExecutedDeliveryRunStatus.SKIPPED_EXECUTION.ToString());
                                }
                                else
                                {
                                    //loop delivery types
                                    foreach (BackupDeliveryConfigTypes deliveryType in Enum.GetValues(typeof(BackupDeliveryConfigTypes)))
                                    {
                                        bool queuedSuccess = await contentDeliveryRecordPersistanceService.AddOrUpdateAsync(new BackupRecordDelivery
                                        {
                                            Id = $"{backupRecord.Id}|{resourceGroup.Id}".ToMD5String().ToUpper(), //Unique Identification
                                            BackupRecordId = backupRecord.Id,
                                            ResourceGroupId = resourceGroup.Id,
                                            CurrentStatus = BackupRecordDeliveryStatus.QUEUED.ToString(),
                                            DeliveryType = deliveryType.ToString(),
                                            RegisteredDateUTC = DateTime.UtcNow,
                                            StatusUpdateDateUTC = DateTime.UtcNow,
                                            ExecutionMessage = "Queued for Dispatch"
                                        });
                                        if (!queuedSuccess)
                                            _logger.LogWarning($"Unable to Queue Backup Record Delivery Id: {backupRecord.Id}, Delivery Type : {deliveryType}");
                                    }

                                    //Update Execution
                                    _ = await backupRecordPersistanceService.UpdateDeliveryRunnedAsync(backupRecord.Id, true, BackupRecordExecutedDeliveryRunStatus.SUCCESSFULLY_EXECUTED.ToString());
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
