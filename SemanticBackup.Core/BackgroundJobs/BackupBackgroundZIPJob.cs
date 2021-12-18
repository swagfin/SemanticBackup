using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupBackgroundZIPJob : IProcessorInitializable
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
                    await Task.Delay(10000);
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                            //Proceed
                            List<BackupRecord> queuedBackups = await backupRecordPersistanceService.GetAllByStatusAsync(BackupRecordBackupStatus.COMPLETED.ToString());
                            if (queuedBackups != null && queuedBackups.Count > 0)
                            {
                                foreach (BackupRecord backupRecord in queuedBackups)
                                {
                                    //Check if valid Resource Group
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdAsync(backupRecord.ResourceGroupId);
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
                                                bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordBackupStatus.COMPRESSING.ToString());
                                                if (updated)
                                                    _logger.LogInformation($"Queueing Zip Database Record Key: #{backupRecord.Id}...SUCCESS");
                                                else
                                                    _logger.LogWarning($"Queued for Zipping But Failed to Update Status for Backup Record Key: #{backupRecord.Id}");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogInformation($">> Skipping Compression for Database Record Key: #{backupRecord.Id}...");
                                            bool updated = await backupRecordPersistanceService.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordBackupStatus.READY.ToString());
                                            if (updated)
                                                _logger.LogInformation($">> Skipped Compression and Completed Backup Updated Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Failed to Update Status as READY for Backup Record Key: #{backupRecord.Id}");
                                        }
                                    }
                                    else
                                        _logger.LogWarning($"The Backup Record Id: {backupRecord.Id}, doesn't seem to have been assigned to a valid Resource Group Id: {backupRecord.ResourceGroupId}, Zipping Skipped");

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
