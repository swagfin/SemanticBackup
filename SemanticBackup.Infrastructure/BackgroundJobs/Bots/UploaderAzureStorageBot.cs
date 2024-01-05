using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderAzureStorageBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderAzureStorageBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _contentDeliveryRecord.Id;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public UploaderAzureStorageBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<UploaderAzureStorageBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Uploading Backup File via Azure Blob Storage....");
                await Task.Delay(new Random().Next(1000));
                AzureBlobStorageDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.AzureBlobStorage ?? throw new Exception("no valid azure blob storage config");
                stopwatch.Start();
                //Upload FTP
                CheckIfFileExistsOrRemove(this._backupRecord.Path);
                //FTP Upload
                string executionMessage = "Azure Blob Storage Uploading...";
                //Container
                string validContainer = (string.IsNullOrWhiteSpace(settings.BlobContainer)) ? "backups" : settings.BlobContainer;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new Exception("Invalid Connection String");
                //Proceed
                using (FileStream stream = File.Open(this._backupRecord.Path, FileMode.Open))
                {
                    BlobContainerClient containerClient = new BlobContainerClient(settings.ConnectionString, validContainer);
                    BlobClient blobClient = containerClient.GetBlobClient(fileName);
                    _ = await blobClient.UploadAsync(stream, true);
                    executionMessage = $"Uploaded Container: {validContainer}";
                }
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordDeliveryStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Uploading Backup File via Azure Blob Storage: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), (ex.InnerException != null) ? $"Error Uploading: {ex.InnerException.Message}" : ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private void CheckIfFileExistsOrRemove(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"No Database File In Path or May have been deleted, Path: {path}");
        }

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    IContentDeliveryRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                    _persistanceService.UpdateStatusFeedAsync(recordId, status, message, elapsed);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error Updating Feed: " + ex.Message);
            }
            finally
            {
                IsCompleted = true;
            }
        }
    }
}
