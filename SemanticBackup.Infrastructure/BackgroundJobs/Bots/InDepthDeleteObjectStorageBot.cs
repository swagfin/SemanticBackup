using Minio;
using Minio.DataModel.Args;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class InDepthDeleteObjectStorageBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(InDepthDeleteObjectStorageBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public InDepthDeleteObjectStorageBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                Console.WriteLine($"deleting uploaded file from ObjectStorage: {_backupRecord.Path}, Id: {_contentDeliveryRecord.Id}");
                //proceed
                ObjectStorageDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.ObjectStorage ?? throw new Exception("no valid object storage config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //Container
                string validBucket = string.IsNullOrWhiteSpace(settings.Bucket) ? "backups" : settings.Bucket;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                using IMinioClient minioClient = new MinioClient().WithEndpoint(settings.Server, settings.Port).WithCredentials(settings.AccessKey, settings.SecretKey).WithSSL(settings.UseSsl).Build();
                {
                    await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                                     .WithBucket(validBucket)
                                     .WithObject(fileName), cancellationToken);
                }
                stopwatch.Stop();
                Console.WriteLine($"Successfully deleted file from ObjectStorage: {_backupRecord.Path}");
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine(ex.Message);
                stopwatch.Stop();
            }
        }
    }
}
