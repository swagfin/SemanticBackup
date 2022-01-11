using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class RestoreBackgroundJob : IProcessorInitializable
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
                    //Delay
                    await Task.Delay(5000);
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                            //Proceed
                            List<BackupRecord> backupRestores = await backupRecordPersistanceService.GetAllByRestoreStatusAsync(BackupRecordRestoreStatus.PENDING_RESTORE.ToString());
                            if (backupRestores != null && backupRestores.Count > 0)
                            {
                                foreach (BackupRecord backupRecord in backupRestores)
                                {
                                    //Check if valid Resource Group
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdAsync(backupRecord.ResourceGroupId);
                                    if (resourceGroup != null)
                                    {
                                        //Use Resource Group Threads
                                        //Check Resource Group Maximum Threads
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                        {
                                            _logger.LogInformation($"Queueing Restore Database Record Key: #{backupRecord.Id}...");
                                            //Add to Queue
                                            //!!!!!!!!!!!!!!!!!!!! NO BOT YET TO ADD !!!!! _botsManagerBackgroundJob.AddBot(new BackupZippingRobot(resourceGroup.Id, backupRecord, _serviceScopeFactory));
                                            bool updated = await backupRecordPersistanceService.UpdateRestoreStatusFeedAsync(backupRecord.Id, BackupRecordRestoreStatus.EXECUTING_RESTORE.ToString());
                                            if (updated)
                                                _logger.LogInformation($"Queueing Restore Database Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Queued for Restore Database Success But Failed to Restore Status for Backup Record Key: #{backupRecord.Id}");
                                        }
                                    }
                                    else
                                        _logger.LogWarning($"The Backup Record Id: {backupRecord.Id}, doesn't seem to have been assigned to a valid Resource Group Id: {backupRecord.ResourceGroupId}, Restore Skipped");

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
