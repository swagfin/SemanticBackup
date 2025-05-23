﻿using Microsoft.Extensions.DependencyInjection;
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
    public class BackupRecordDeliveryDispatchBackgroundJob : IHostedService
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public BackupRecordDeliveryDispatchBackgroundJob(ILogger<BackupBackgroundJob> logger, IServiceScopeFactory serviceScopeFactory, BotsManagerBackgroundJob botsManagerBackgroundJob)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _botsManagerBackgroundJob = botsManagerBackgroundJob;
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
            Thread t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(10000, cancellationToken);
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI INJECTIONS
                        IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                        IDatabaseInfoRepository databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();
                        //Proceed
                        List<BackupRecordDelivery> contentDeliveryRecords = await contentDeliveryRecordPersistanceService.GetAllByStatusAsync(BackupRecordDeliveryStatus.QUEUED.ToString());
                        if (contentDeliveryRecords != null && contentDeliveryRecords.Count > 0)
                        {
                            List<string> scheduleToDeleteRecords = [];
                            foreach (BackupRecordDelivery contentDeliveryRecord in contentDeliveryRecords.OrderBy(x => x.RegisteredDateUTC).ToList())
                            {
                                _logger.LogInformation($"Processing Queued Content Delivery Record: #{contentDeliveryRecord.Id}...");
                                BackupRecord backupRecordInfo = await backupRecordPersistanceService.GetByIdAsync(contentDeliveryRecord?.BackupRecordId ?? 0);
                                BackupDatabaseInfo backupDatabaseInfo = await databaseInfoPersistanceService.GetByIdAsync(backupRecordInfo?.BackupDatabaseInfoId);
                                ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(backupDatabaseInfo?.ResourceGroupId);

                                if (backupRecordInfo == null)
                                {
                                    _logger.LogWarning($"No Backup Record with Id: {contentDeliveryRecord.BackupRecordId}, Content Delivery Record will be Deleted: {contentDeliveryRecord.Id}");
                                    scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                }
                                else if (resourceGroup == null)
                                {
                                    _logger.LogWarning($"Backup Record Id: {contentDeliveryRecord.BackupRecordId}, Queued for Content Delivery has no valid Resource Group, Will be Removed");
                                    scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                }
                                else if (resourceGroup.BackupDeliveryConfig == null)
                                {
                                    _logger.LogWarning($"Backup Record Id: {contentDeliveryRecord.BackupRecordId}, Queued for Content Delivery has no valid Configuration, Will be Removed");
                                    scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                }
                                else
                                {
                                    //Override Maximum Running Threads// This is because of currently being used exception
                                    if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, resourceGroup.MaximumRunningBots))
                                    {
                                        string status = BackupRecordDeliveryStatus.EXECUTING.ToString();
                                        string statusMsg = "Dispatching Backup Record";
                                        if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.DownloadLink.ToString())
                                        {
                                            //Download Link Generator
                                            _botsManagerBackgroundJob.AddBot(new UploaderLinkGenBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.Ftp.ToString())
                                        {
                                            //FTP Uploader
                                            _botsManagerBackgroundJob.AddBot(new UploaderFTPBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.Smtp.ToString())
                                        {
                                            //Email Send and Uploader
                                            _botsManagerBackgroundJob.AddBot(new UploaderEmailSMTPBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.Dropbox.ToString())
                                        {
                                            //Email Send and Uploader
                                            _botsManagerBackgroundJob.AddBot(new UploaderDropboxBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.AzureBlobStorage.ToString())
                                        {
                                            //Azure Blob Storage
                                            _botsManagerBackgroundJob.AddBot(new UploaderAzureStorageBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else if (contentDeliveryRecord.DeliveryType == BackupDeliveryConfigTypes.ObjectStorage.ToString())
                                        {
                                            //Object Storage
                                            _botsManagerBackgroundJob.AddBot(new UploaderObjectStorageBot(resourceGroup, backupRecordInfo, contentDeliveryRecord, _serviceScopeFactory));
                                        }
                                        else
                                        {
                                            status = BackupRecordDeliveryStatus.ERROR.ToString();
                                            statusMsg = $"Backup Record Id: {contentDeliveryRecord.BackupRecordId}, Queued for Content Delivery has UNSUPPORTED Delivery Type, Record Will be Removed";
                                            _logger.LogWarning(statusMsg);
                                            scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                        }
                                        //Finally Update Status
                                        bool updated = await contentDeliveryRecordPersistanceService.UpdateStatusFeedAsync(contentDeliveryRecord.Id, status, statusMsg);
                                        if (!updated)
                                            _logger.LogWarning($"Queued for Backup but was unable to update backup record Key: #{contentDeliveryRecord.Id} status");
                                    }
                                    else
                                        _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} Bots are Busy, Running Bots Count: {resourceGroup.MaximumRunningBots}, waiting for available Bots....");
                                }
                            }
                            //Check if Any Delete
                            if (scheduleToDeleteRecords.Count > 0)
                                foreach (var rm in scheduleToDeleteRecords)
                                    await contentDeliveryRecordPersistanceService.RemoveAsync(rm);
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
            Thread t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(60000, cancellationToken); //Runs After 1 Minute
                    try
                    {
                        using IServiceScope scope = _serviceScopeFactory.CreateScope();
                        //DI INJECTIONS
                        IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                        //Proceed
                        List<BackupRecord> expiredBackups = await backupRecordPersistanceService.GetAllExpiredAsync();
                        if (expiredBackups != null && expiredBackups.Count > 0)
                        {
                            List<long> toDeleteList = new();
                            foreach (BackupRecord backupRecord in expiredBackups)
                                toDeleteList.Add(backupRecord.Id);
                            _logger.LogInformation($"Queued ({expiredBackups.Count}) Expired Records for Delete");
                            //Check if Any Delete
                            if (toDeleteList.Count > 0)
                                foreach (var rm in toDeleteList)
                                    if (!(await backupRecordPersistanceService.RemoveAsync(rm)))
                                        _logger.LogWarning("Unable to delete Expired Backup Record");
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
