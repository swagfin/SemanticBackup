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
    public class BackupBackgroundZIPJob : IHostedService
    {
        private readonly ILogger<BackupBackgroundZIPJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupBackgroundZIPJob(
            ILogger<BackupBackgroundZIPJob> logger,
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
            Thread t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(7000, cancellationToken);
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI INJECTIONS
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                        IDatabaseInfoRepository databaseInfoRepository = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                        //Proceed
                        List<BackupRecord> queuedBackups = await backupRecordPersistanceService.GetAllByStatusAsync(BackupRecordStatus.COMPLETED.ToString());
                        if (queuedBackups != null && queuedBackups.Count > 0)
                        {
                            foreach (BackupRecord backupRecord in queuedBackups.OrderBy(x => x.RegisteredDateUTC).ToList())
                            {
                                //get valid database
                                BackupDatabaseInfo backupRecordDbInfo = await databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                //Check if valid Resource Group
                                ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                                if (resourceGroup != null)
                                {
                                    //Use Resource Group Threads
                                    if (resourceGroup.CompressBackupFiles)
                                    {
                                        //Check Resource Group Maximum Threads
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                        {
                                            _logger.LogInformation($"Queueing Zip Database Record Key: #{backupRecord.Id}...");
                                            //Add to Queue
                                            _botsManagerBackgroundJob.AddBot(new BackupZippingRobot(resourceGroup.Id, backupRecord, _serviceScopeFactory));
                                            bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.COMPRESSING.ToString());
                                            if (updated)
                                                _logger.LogInformation($"Queueing Zip Database Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Queued for Zipping But Failed to Update Status for Backup Record Key: #{backupRecord.Id}");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation($">> Skipping Compression for Database Record Key: #{backupRecord.Id}...");
                                        bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.READY.ToString());
                                        if (updated)
                                            _logger.LogInformation($">> Skipped Compression and Completed Backup Updated Record Key: #{backupRecord.Id}...SUCCESS");
                                        else
                                            _logger.LogWarning($"Failed to Update Status as READY for Backup Record Key: #{backupRecord.Id}");
                                    }
                                }
                                else
                                    _logger.LogWarning($"The Backup Record Id: {backupRecord.Id}, doesn't seem to have been assigned to a valid Resource Group, Zipping Skipped");
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
