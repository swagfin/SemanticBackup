using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class RestoreBackgroundJob : IHostedService
    {
        private readonly ILogger<RestoreBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public RestoreBackgroundJob(
            ILogger<RestoreBackgroundJob> logger,
            IServiceScopeFactory serviceScopeFactory,
            BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            this._logger = logger;
            this._serviceScopeFactory = serviceScopeFactory;
            this._botsManagerBackgroundJob = botsManagerBackgroundJob;
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
                    await Task.Delay(5000);
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                            IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                            IDatabaseInfoRepository databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                            //Proceed
                            List<BackupRecord> backupRestores = await backupRecordPersistanceService.GetAllByRestoreStatusAsync(BackupRecordRestoreStatus.PENDING_RESTORE.ToString());
                            if (backupRestores != null && backupRestores.Count > 0)
                            {
                                foreach (BackupRecord backupRecord in backupRestores.OrderBy(x => x.RegisteredDateUTC))
                                {
                                    _logger.LogInformation($"Processing Queued Backup RESTORE Record Key: #{backupRecord.Id}...");
                                    BackupDatabaseInfo backupDatabaseInfo = await databaseInfoPersistanceService.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupDatabaseInfo.ResourceGroupId);
                                    if (backupDatabaseInfo != null && resourceGroup != null)
                                    {
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                        {
                                            if (resourceGroup.DbType.Contains("SQLSERVER"))
                                                _botsManagerBackgroundJob.AddBot(new SQLRestoreBot(backupDatabaseInfo.DatabaseName, resourceGroup, backupRecord, _serviceScopeFactory));
                                            else if (resourceGroup.DbType.Contains("MYSQL") || resourceGroup.DbType.Contains("MARIADB"))
                                                throw new Exception("No RESTORE Bot for MYSQL");
                                            else
                                                throw new Exception($"No Bot is registered to Handle Database RESTORE of Type: {resourceGroup.DbType}");
                                            //Finally Update Status
                                            bool updated = await backupRecordPersistanceService.UpdateRestoreStatusFeedAsync(backupRecord.Id, BackupRecordRestoreStatus.EXECUTING_RESTORE.ToString(), "Executing Restore....");
                                            if (updated)
                                                _logger.LogInformation($"Processing Queued Backup RESTORE Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Queued for Backup RESTORE but was unable to update backup record Key: #{backupRecord.Id} status");
                                        }
                                        else
                                            _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} has Exceeded its Maximum Allocated Running Threads Count: {resourceGroup.MaximumRunningBots}");
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
