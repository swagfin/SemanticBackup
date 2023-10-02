using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class InDepthDeleteAzureStorageBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly ContentDeliveryRecord _contentDeliveryRecord;
        private readonly BackupRecord _backupRecord;
        private readonly ContentDeliveryConfiguration _contentDeliveryConfiguration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderAzureStorageBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;
        public string BotId => _contentDeliveryRecord.Id;
        public InDepthDeleteAzureStorageBot(string resourceGroupId, BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IServiceScopeFactory scopeFactory)
        {
            this._resourceGroupId = resourceGroupId;
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._backupRecord = backupRecord;
            this._contentDeliveryConfiguration = contentDeliveryConfiguration;
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
                _logger.LogInformation($"DELETING Backup File From Azure Blob Storage....");
                await Task.Delay(new Random().Next(1000));
                RSAzureBlobStorageSetting settings = GetValidDeserializedSettings();
                stopwatch.Start();
                //Container
                string validContainer = (string.IsNullOrWhiteSpace(settings.BlobContainer)) ? "backups" : settings.BlobContainer;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new Exception("Invalid Connection String");
                //Proceed
                CloudStorageAccount account = CloudStorageAccount.Parse(settings.ConnectionString);
                var blobClient = account.CreateCloudBlobClient();
                // Make sure container is there
                var blobContainer = blobClient.GetContainerReference(validContainer);
                if (blobContainer != null)
                {
                    CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(fileName);
                    await blockBlob.DeleteIfExistsAsync(); //Removes Blob Reference
                }
                stopwatch.Stop();
                _logger.LogInformation($"DELETING Backup File From Azure Blob Storage: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex.Message);
                stopwatch.Stop();
            }
            finally
            {
                this.IsCompleted = true;
            }
        }

        private RSAzureBlobStorageSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSAzureBlobStorageSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSAzureBlobStorageSetting)}");
            return config;
        }
    }
}
