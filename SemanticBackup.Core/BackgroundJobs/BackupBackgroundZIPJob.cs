using Microsoft.Extensions.Logging;
using SemanticBackup.Core.BackgroundJobs.Bots;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs
{
    public class BackupBackgroundZIPJob : IProcessorInitializable
    {
        private readonly ILogger<BackupBackgroundZIPJob> _logger;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;

        internal List<IBot> BackupZippingBots { get; private set; }

        public BackupBackgroundZIPJob(ILogger<BackupBackgroundZIPJob> logger,
            PersistanceOptions persistanceOptions,
            IBackupRecordPersistanceService backupRecordPersistanceService, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this._logger = logger;
            this._persistanceOptions = persistanceOptions;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this.BackupZippingBots = new List<IBot>();
        }

        public void Initialize()
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService();
            SetupBotsZippingBackgroundService();
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
                        List<BackupRecord> queuedBackups = this._backupRecordPersistanceService.GetAllByStatus(BackupRecordBackupStatus.COMPLETED.ToString());
                        if (queuedBackups != null)
                        {
                            foreach (BackupRecord backupRecord in queuedBackups)
                            {
                                //Check if valid Resource Group
                                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupRecord.ResourceGroupId);
                                if (resourceGroup != null)
                                {
                                    //Use Resource Group Threads
                                    if (this._persistanceOptions.CompressBackupFiles)
                                    {
                                        //Check Resource Group Maximum Threads
                                        int runningResourceGrpThreads = this.BackupZippingBots.Where(x => x.resourceGroupId == resourceGroup.Id).Count(x => x.IsStarted && !x.IsCompleted);
                                        int availableResourceGrpThreads = resourceGroup.MaximumBackupRunningThreads - runningResourceGrpThreads;
                                        if (availableResourceGrpThreads > 0)
                                        {
                                            _logger.LogInformation($"Queueing Zip Database Record Key: #{backupRecord.Id}...");
                                            //Add to Queue
                                            BackupZippingBots.Add(new BackupZippingRobot(resourceGroup.Id, backupRecord, _backupRecordPersistanceService, _logger));
                                            bool updated = this._backupRecordPersistanceService.UpdateStatusFeed(backupRecord.Id, BackupRecordBackupStatus.COMPRESSING.ToString());
                                            if (updated)
                                                _logger.LogInformation($"Queueing Zip Database Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Queued for Zipping But Failed to Update Status for Backup Record Key: #{backupRecord.Id}");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation($">> Skipping Compression for Database Record Key: #{backupRecord.Id}...");
                                        bool updated = this._backupRecordPersistanceService.UpdateStatusFeed(backupRecord.Id, BackupRecordBackupStatus.READY.ToString());
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    //Delay
                    await Task.Delay(10000);
                }
            });
            t.Start();
        }

        private void SetupBotsZippingBackgroundService()
        {
            var t = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (this.BackupZippingBots != null || this.BackupZippingBots.Count > 0)
                        {
                            //Start and Stop Bacup Bots
                            int runningThreads = this.BackupZippingBots.Count(x => x.IsStarted && !x.IsCompleted);
                            int takeCount = _persistanceOptions.MaximumBackupRunningThreads - runningThreads;
                            if (takeCount > 0)
                            {
                                List<IBot> botsNotStarted = this.BackupZippingBots.Where(x => !x.IsStarted).Take(takeCount).ToList();
                                if (botsNotStarted != null && botsNotStarted.Count > 0)
                                    foreach (IBot bot in botsNotStarted)
                                        _ = bot.RunAsync();
                            }
                            //Remove Completed
                            List<IBot> botsCompleted = this.BackupZippingBots.Where(x => x.IsCompleted).ToList();
                            if (botsCompleted != null && botsCompleted.Count > 0)
                                foreach (IBot bot in botsCompleted)
                                    this.BackupZippingBots.Remove(bot);
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning($"Running Unstarted and Removing Completed Bots Failed: {ex.Message}"); }
                    //Delay
                    await Task.Delay(2000);
                }
            });
            t.Start();
        }

    }
}
