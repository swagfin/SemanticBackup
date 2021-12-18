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
    public class ContentDeliveryDispatchBackgroundJob : IProcessorInitializable
    {
        private readonly ILogger<BackupBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly BotsManagerBackgroundJob _botsManagerBackgroundJob;

        public ContentDeliveryDispatchBackgroundJob(
            ILogger<BackupBackgroundJob> logger,
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
            SetupBackgroundRemovedExpiredBackupsService();
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
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordPersistanceService>();
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            IResourceGroupPersistanceService resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupPersistanceService>();
                            IContentDeliveryConfigPersistanceService contentDeliveryConfigPersistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryConfigPersistanceService>();

                            //Proceed
                            List<ContentDeliveryRecord> contentDeliveryRecords = await contentDeliveryRecordPersistanceService.GetAllByStatusAsync(ContentDeliveryRecordStatus.QUEUED.ToString());
                            if (contentDeliveryRecords != null && contentDeliveryRecords.Count > 0)
                            {
                                List<string> scheduleToDeleteRecords = new List<string>();
                                foreach (ContentDeliveryRecord contentDeliveryRecord in contentDeliveryRecords)
                                {
                                    _logger.LogInformation($"Processing Queued Content Delivery Record: #{contentDeliveryRecord.Id}...");
                                    BackupRecord backupRecordInfo = await backupRecordPersistanceService.GetByIdAsync(contentDeliveryRecord?.BackupRecordId);
                                    ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdAsync(backupRecordInfo?.ResourceGroupId);
                                    ContentDeliveryConfiguration contentDeliveryConfiguration = await contentDeliveryConfigPersistanceService.GetByIdAsync(contentDeliveryRecord?.ContentDeliveryConfigurationId);

                                    if (backupRecordInfo == null)
                                    {
                                        _logger.LogWarning($"No Backup Record with Id: {contentDeliveryRecord.BackupRecordId}, Content Delivery Record will be Deleted: {contentDeliveryRecord.Id}");
                                        scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                    }
                                    else if (contentDeliveryConfiguration == null)
                                    {
                                        _logger.LogWarning($"Backup Record Id: {contentDeliveryRecord.BackupRecordId}, Queued for Content Delivery has no valid Configuration, Will be Removed");
                                        scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                    }
                                    else if (resourceGroup == null)
                                    {
                                        _logger.LogWarning($"Backup Record Id: {contentDeliveryRecord.BackupRecordId}, Queued for Content Delivery has no valid Resource Group, Will be Removed");
                                        scheduleToDeleteRecords.Add(contentDeliveryRecord.Id);
                                    }
                                    else
                                    {
                                        //Override Maximum Running Threads// This is because of currently being used exception
                                        if (_botsManagerBackgroundJob.HasAvailableResourceGroupBotsCount(resourceGroup.Id, 1))
                                        {
                                            string status = ContentDeliveryRecordStatus.EXECUTING.ToString();
                                            string statusMsg = "Dispatching Backup Record";
                                            if (contentDeliveryRecord.DeliveryType == ContentDeliveryType.DIRECT_LINK.ToString())
                                            {
                                                //Download Link Generator
                                                _botsManagerBackgroundJob.AddBot(new UploaderLinkGenBot(backupRecordInfo, contentDeliveryRecord, contentDeliveryConfiguration, _serviceScopeFactory));
                                            }
                                            else if (contentDeliveryRecord.DeliveryType == ContentDeliveryType.FTP_UPLOAD.ToString())
                                            {
                                                //FTP Uploader
                                                _botsManagerBackgroundJob.AddBot(new UploaderFTPBot(backupRecordInfo, contentDeliveryRecord, contentDeliveryConfiguration, _serviceScopeFactory));
                                            }
                                            else if (contentDeliveryRecord.DeliveryType == ContentDeliveryType.EMAIL_SMTP.ToString())
                                            {
                                                //Email Send and Uploader
                                                _botsManagerBackgroundJob.AddBot(new UploaderEmailSMTPBot(backupRecordInfo, contentDeliveryRecord, contentDeliveryConfiguration, _serviceScopeFactory));
                                            }
                                            else if (contentDeliveryRecord.DeliveryType == ContentDeliveryType.DROPBOX.ToString())
                                            {
                                                //Email Send and Uploader
                                                _botsManagerBackgroundJob.AddBot(new UploaderDropboxBot(backupRecordInfo, contentDeliveryRecord, contentDeliveryConfiguration, _serviceScopeFactory));
                                            }
                                            else if (contentDeliveryRecord.DeliveryType == ContentDeliveryType.AZURE_BLOB_STORAGE.ToString())
                                            {
                                                //Azure Blob Storage
                                                _botsManagerBackgroundJob.AddBot(new UploaderAzureStorageBot(backupRecordInfo, contentDeliveryRecord, contentDeliveryConfiguration, _serviceScopeFactory));
                                            }
                                            else
                                            {
                                                status = ContentDeliveryRecordStatus.ERROR.ToString();
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
                                            _logger.LogInformation($"Resource Group With Id: {resourceGroup.Id} has Exceeded its Maximum Allocated Running Threads Count: {resourceGroup.MaximumRunningBots}");
                                    }
                                }
                                //Check if Any Delete
                                if (scheduleToDeleteRecords.Count > 0)
                                    foreach (var rm in scheduleToDeleteRecords)
                                        await contentDeliveryRecordPersistanceService.RemoveAsync(rm);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    //Await
                    await Task.Delay(5000);
                }
            });
            t.Start();
        }

        private void SetupBackgroundRemovedExpiredBackupsService()
        {
            var t = new Thread(async () =>
            {
                while (true)
                {
                    await Task.Delay(60000); //Runs After 1 Minute
                    try
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            //DI INJECTIONS
                            IBackupRecordPersistanceService backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                            //Proceed
                            List<BackupRecord> expiredBackups = await backupRecordPersistanceService.GetAllExpiredAsync();
                            if (expiredBackups != null && expiredBackups.Count > 0)
                            {
                                List<string> toDeleteList = new List<string>();
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
