using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
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
    public class BackupBackgroundJob : IHostedService
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        private readonly IBackupProviderForSQLServer _providerForSQLServer;
        private readonly IBackupProviderForMySQLServer _providerForMySqlServer;

        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _deliveryRecordRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        public BackupBackgroundJob(
            ILogger<BackupBackgroundJob> logger,
            SystemConfigOptions persistanceOptions,
            BotsManagerBackgroundJob botsManagerBackgroundJob,
            IBackupProviderForSQLServer providerForSQLServer,
            IBackupProviderForMySQLServer providerForMySqlServer,

            IResourceGroupRepository resourceGroupRepository,
            IBackupRecordRepository backupRecordRepository,
            IContentDeliveryRecordRepository contentDeliveryRecordRepository,
            IDatabaseInfoRepository databaseInfoRepository
            )
        {
            _logger = logger;
            _persistanceOptions = persistanceOptions;
            _botsManagerBackgroundJob = botsManagerBackgroundJob;
            _providerForSQLServer = providerForSQLServer;
            _providerForMySqlServer = providerForMySqlServer;
            _resourceGroupRepository = resourceGroupRepository;
            _backupRecordRepository = backupRecordRepository;
            _deliveryRecordRepository = contentDeliveryRecordRepository;
            _databaseInfoRepository = databaseInfoRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service....");
            SetupBackgroundService(cancellationToken);
            SetupBackgroundRemovedExpiredBackupsService(cancellationToken);
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
                    //Await
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    try
                    {
                        //Proceed
                        List<BackupRecord> queuedBackups = await _backupRecordRepository.GetAllByStatusAsync(BackupRecordStatus.QUEUED.ToString());
                        if (queuedBackups != null && queuedBackups.Count > 0)
                        {
                            List<long> scheduleToDelete = [];
                            foreach (BackupRecord backupRecord in queuedBackups.OrderBy(x => x.RegisteredDateUTC).ToList())
                            {
                                _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...");
                                BackupDatabaseInfo backupDatabaseInfo = await _databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                                if (backupDatabaseInfo == null)
                                {
                                    _logger.LogWarning($"No Database Info matches with Id: {backupRecord.BackupDatabaseInfoId}, Backup Database Record will be Deleted: {backupRecord.Id}");
                                    scheduleToDelete.Add(backupRecord.Id);
                                }
                                else
                                {
                                    //Check if valid Resource Group
                                    ResourceGroup resourceGroup = await _resourceGroupRepository.GetByIdOrKeyAsync(backupDatabaseInfo.ResourceGroupId);
                                    if (resourceGroup == null)
                                    {
                                        _logger.LogWarning($"The Database Id: {backupRecord.BackupDatabaseInfoId}, doesn't seem to have been assigned to a valid Resource Group Id: {backupDatabaseInfo.ResourceGroupId}, Record will be Deleted");
                                        scheduleToDelete.Add(backupRecord.Id);
                                    }
                                    else
                                    {
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                        {
                                            if (resourceGroup.DbType.Contains("SQLSERVER"))
                                                _botsManagerBackgroundJob.AddBot(new SQLBackupBot(backupDatabaseInfo.DatabaseName, resourceGroup, backupRecord, _providerForSQLServer));
                                            else if (resourceGroup.DbType.Contains("MYSQL") || resourceGroup.DbType.Contains("MARIADB"))
                                                _botsManagerBackgroundJob.AddBot(new MySQLBackupBot(backupDatabaseInfo.DatabaseName, resourceGroup, backupRecord, _providerForMySqlServer));
                                            else
                                                throw new Exception($"No Bot is registered to Handle Database Backups of Type: {resourceGroup.DbType}");
                                            //Finally Update Status
                                            bool updated = await _backupRecordRepository.UpdateStatusFeedAsync(backupRecord.Id, BackupRecordStatus.EXECUTING.ToString());
                                            if (updated)
                                                _logger.LogInformation($"Processing Queued Backup Record Key: #{backupRecord.Id}...SUCCESS");
                                            else
                                                _logger.LogWarning($"Queued for Backup but was unable to update backup record Key: #{backupRecord.Id} status");
                                        }
                                        else
                                            _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} Bots are Busy, Running Bots Count: {resourceGroup.MaximumRunningBots}, waiting for available Bots....");
                                    }

                                }
                            }
                            //Check if Any Delete
                            foreach (var rm in scheduleToDelete)
                                await _backupRecordRepository.RemoveWithFileAsync(rm);
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

        private void SetupBackgroundRemovedExpiredBackupsService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    try
                    {
                        //Proceed
                        List<BackupRecord> expiredBackups = (await _backupRecordRepository.GetAllExpiredAsync()) ?? [];
                        //proceed
                        foreach (BackupRecord rm in expiredBackups.Take(50).ToList())
                        {
                            //get relation 
                            List<BackupRecordDelivery> rmBackupRecords = (await _deliveryRecordRepository.GetAllByBackupRecordIdAsync(rm.Id)) ?? [];
                            //remove with file
                            await _backupRecordRepository.RemoveWithFileAsync(rm.Id);
                            //Options InDepth Delete
                            if (_persistanceOptions.InDepthBackupRecordDeleteEnabled)
                                await StartInDepthDeleteForAsync(rm, rmBackupRecords);
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

        private async Task StartInDepthDeleteForAsync(BackupRecord backupRecord, List<BackupRecordDelivery> rmBackupRecords)
        {
            try
            {
                if (backupRecord == null) return;
                //get db information
                BackupDatabaseInfo backupRecordDbInfo = await _databaseInfoRepository.GetByIdAsync(backupRecord.BackupDatabaseInfoId);
                //Check if valid Resource Group
                ResourceGroup resourceGroup = await _resourceGroupRepository.GetByIdOrKeyAsync(backupRecordDbInfo?.ResourceGroupId ?? string.Empty);
                if (resourceGroup == null)
                    return;
                //Proceed
                if (rmBackupRecords == null)
                    return;
                List<string> supportedInDepthDelete = [BackupDeliveryConfigTypes.Dropbox.ToString(), BackupDeliveryConfigTypes.AzureBlobStorage.ToString()];
                List<BackupRecordDelivery> supportedDeliveryRecords = [.. rmBackupRecords.Where(x => supportedInDepthDelete.Contains(x.DeliveryType))];
                if (supportedDeliveryRecords == null || supportedDeliveryRecords.Count == 0)
                    return;
                foreach (BackupRecordDelivery deliveryRecord in supportedDeliveryRecords)
                {
                    if (deliveryRecord.DeliveryType == BackupDeliveryConfigTypes.Dropbox.ToString())
                    {
                        //In Depth Remove From DropBox
                        _botsManagerBackgroundJob.AddBot(new InDepthDeleteDropboxBot(resourceGroup, backupRecord, deliveryRecord));
                    }
                    else if (deliveryRecord.DeliveryType == BackupDeliveryConfigTypes.ObjectStorage.ToString())
                    {
                        //In Depth Remove From Object Storage
                        _botsManagerBackgroundJob.AddBot(new InDepthDeleteObjectStorageBot(resourceGroup, backupRecord, deliveryRecord));
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
        }
    }
}
