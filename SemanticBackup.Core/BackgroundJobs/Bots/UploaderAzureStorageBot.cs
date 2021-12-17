using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class UploaderAzureStorageBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly ContentDeliveryRecord _contentDeliveryRecord;
        private readonly BackupRecord _backupRecord;
        private readonly ContentDeliveryConfiguration _contentDeliveryConfiguration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;
        public string BotId => _contentDeliveryRecord.Id;
        public UploaderAzureStorageBot(BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IServiceScopeFactory scopeFactory, ILogger logger)
        {
            this._resourceGroupId = backupRecord.ResourceGroupId;
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._backupRecord = backupRecord;
            this._contentDeliveryConfiguration = contentDeliveryConfiguration;
            this._scopeFactory = scopeFactory;
            this._logger = logger;
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
                RSAzureBlobStorageSetting settings = GetValidDeserializedSettings();
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
                    CloudStorageAccount account = CloudStorageAccount.Parse(settings.ConnectionString);
                    var blobClient = account.CreateCloudBlobClient();
                    // Make sure container is there
                    var blobContainer = blobClient.GetContainerReference(validContainer);
                    await blobContainer.CreateIfNotExistsAsync();
                    CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(fileName);
                    await blockBlob.UploadFromStreamAsync(stream);
                    executionMessage = $"Uploaded Container: {validContainer}";
                }
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, ContentDeliveryRecordStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Uploading Backup File via Azure Blob Storage: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), (ex.InnerException != null) ? $"Error Uploading: {ex.InnerException.Message}" : ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private RSAzureBlobStorageSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSAzureBlobStorageSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSAzureBlobStorageSetting)}");
            return config;
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
                    IContentDeliveryRecordPersistanceService _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordPersistanceService>();
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
