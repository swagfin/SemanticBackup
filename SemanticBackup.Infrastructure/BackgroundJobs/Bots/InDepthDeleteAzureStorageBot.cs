using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class InDepthDeleteAzureStorageBot : IBot
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
        public InDepthDeleteAzureStorageBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
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
                _logger.LogInformation($"DELETING Backup File From Azure Blob Storage....");
                await Task.Delay(new Random().Next(1000));
                AzureBlobStorageDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.AzureBlobStorage ?? throw new Exception("no valid azure blob storage config");
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
    }
}
