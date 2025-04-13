using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BackupRecordDeliverySchedulerBackgroundJob : IHostedService
    {
        private readonly ILogger<BackupRecordDeliverySchedulerBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public BackupRecordDeliverySchedulerBackgroundJob(
            ILogger<BackupRecordDeliverySchedulerBackgroundJob> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this._logger = logger;
            this._serviceScopeFactory = serviceScopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService(cancellationToken);
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
                    await Task.Delay(4000, cancellationToken);
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
                                        bool isDeliveryEnabled = false;
                                        switch (deliveryType)
                                        {
                                            case BackupDeliveryConfigTypes.DownloadLink: isDeliveryEnabled = resourceGroup.BackupDeliveryConfig.DownloadLink?.IsEnabled ?? false; break;
                                            case BackupDeliveryConfigTypes.Ftp: isDeliveryEnabled = resourceGroup.BackupDeliveryConfig.Ftp?.IsEnabled ?? false; break;
                                            case BackupDeliveryConfigTypes.Smtp: isDeliveryEnabled = resourceGroup.BackupDeliveryConfig.Smtp?.IsEnabled ?? false; break;
                                            case BackupDeliveryConfigTypes.Dropbox: isDeliveryEnabled = resourceGroup.BackupDeliveryConfig.Dropbox?.IsEnabled ?? false; break;
                                            case BackupDeliveryConfigTypes.AzureBlobStorage: isDeliveryEnabled = resourceGroup.BackupDeliveryConfig.AzureBlobStorage?.IsEnabled ?? false; break;
                                            default: isDeliveryEnabled = false; break;
                                        }
                                        //check if enabled
                                        if (isDeliveryEnabled)
                                        {
                                            bool queuedSuccess = await contentDeliveryRecordPersistanceService.AddOrUpdateAsync(new BackupRecordDelivery
                                            {
                                                Id = $"{backupRecord.Id}|{resourceGroup.Id}|{deliveryType}".ToMD5String().ToUpper(), //Unique Identification
                                                BackupRecordId = backupRecord.Id,
                                                CurrentStatus = BackupRecordDeliveryStatus.QUEUED.ToString(),
                                                DeliveryType = deliveryType.ToString(),
                                                RegisteredDateUTC = DateTime.UtcNow,
                                                StatusUpdateDateUTC = DateTime.UtcNow,
                                                ExecutionMessage = "Queued for Dispatch"
                                            });
                                            if (!queuedSuccess)
                                                _logger.LogWarning($"unable to queue Backup Record Id: {backupRecord.Id} for delivery via : {deliveryType}, resource group: {resourceGroup.Name}");
                                        }
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
