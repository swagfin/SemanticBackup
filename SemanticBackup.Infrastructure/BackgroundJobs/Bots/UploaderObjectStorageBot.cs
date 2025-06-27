using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderObjectStorageBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(UploaderObjectStorageBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public UploaderObjectStorageBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord)
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
                Console.WriteLine($"uploading file to ObjectStorage: {_backupRecord.Path}");
                //proceed
                ObjectStorageDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.ObjectStorage ?? throw new Exception("no valid object storage config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //check path
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or May have been deleted, Path: {_backupRecord.Path}");
                //proceed
                string executionMessage = "Object Storage Uploading...";
                //Container
                string validBucket = string.IsNullOrWhiteSpace(settings.Bucket) ? "backups" : settings.Bucket;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                using IMinioClient minioClient = new MinioClient().WithEndpoint(settings.Server, settings.Port).WithCredentials(settings.AccessKey, settings.SecretKey).WithSSL(settings.UseSsl).Build();
                using (FileStream stream = File.Open(_backupRecord.Path, FileMode.Open))
                {
                    //upload object
                    PutObjectResponse putResponse = await minioClient.PutObjectAsync(new PutObjectArgs()
                                                    .WithBucket(settings.Bucket)
                                                    .WithObject(fileName)
                                                    .WithStreamData(stream)
                                                    .WithObjectSize(stream.Length), cancellationToken);
                    //proceed
                    if (putResponse.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        executionMessage = $"Failed: {putResponse.ResponseStatusCode}";
                        throw new Exception(executionMessage);
                    }
                    //proceed
                    executionMessage = $"Uploaded to Bucket: {validBucket}";
                }
                stopwatch.Stop();
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupDeliveryNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = _contentDeliveryRecord.Id,
                    Status = BackupRecordStatus.READY,
                    Message = executionMessage,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);

                Console.WriteLine($"Successfully uploaded file to ObjectStorage: {_backupRecord.Path}");
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine(ex.Message);
                stopwatch.Stop();
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupDeliveryNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = _contentDeliveryRecord.Id,
                    Status = BackupRecordStatus.ERROR,
                    Message = (ex.InnerException != null) ? $"Error: {ex.InnerException.Message}" : ex.Message,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);
            }
        }
    }
}
