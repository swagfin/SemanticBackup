using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BackupBackgroundZIPJob : IHostedService
    {
        private readonly ILogger<BackupBackgroundZIPJob> _logger;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;

        public BackupBackgroundZIPJob(
            ILogger<BackupBackgroundZIPJob> logger,
            BotsManagerBackgroundJob botsManagerBackgroundJob,

            IResourceGroupRepository resourceGroupRepository,
            IDatabaseInfoRepository databaseInfoRepository,
            IBackupRecordRepository backupRecordRepository
            )
        {
            this._logger = logger;
            this._botsManagerBackgroundJob = botsManagerBackgroundJob;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoRepository = databaseInfoRepository;
            this._backupRecordRepository = backupRecordRepository;
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
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    try
                    {
                        //Proceed
                        List<BackupRecord> queuedBackups = (await _backupRecordRepository.GetAllByStatusAsync(BackupRecordStatus.COMPLETED.ToString())) ?? [];
                        foreach (BackupRecord backupRecord in queuedBackups.OrderBy(x => x.Id).ToList())
                        {
                            //get valid database
                            BackupDatabaseInfo backupRecordDbInfo = await _databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                            //Check if valid Resource Group
                            ResourceGroup resourceGroup = await _resourceGroupRepository.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                            if (resourceGroup != null)
                            {
                                //Use Resource Group Threads
                                if (resourceGroup.CompressBackupFiles)
                                {
                                    //Check Resource Group Maximum Threads
                                    if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                    {
                                        _logger.LogInformation("Queueing Zip Database Record Key: #{Id}...", backupRecord.Id);
                                        //Add to Queue
                                        _botsManagerBackgroundJob.AddBot(new BackupZippingBot(resourceGroup.Id, backupRecord));
                                        //Finally Update Status
                                        _ = await _backupRecordRepository.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.COMPRESSING.ToString());
                                    }
                                    else
                                        Debug.WriteLine($"[{nameof(BackupBackgroundZIPJob)}] Resource Group({resourceGroup.Id}) Bots are Busy, Running Bots: {resourceGroup.MaximumRunningBots}, waiting for available Bots....");
                                }
                                else
                                {
                                    _logger.LogInformation(">> Skipping Compression for Database Record Key: #{Id}...", backupRecord.Id);
                                    //Finally Update Status
                                    _ = await _backupRecordRepository.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.READY.ToString());
                                }
                            }
                            else
                                _logger.LogWarning("The Backup Record Id: {Id}, doesn't seem to have been assigned to a valid Resource Group, Zipping Skipped", backupRecord.Id);
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
