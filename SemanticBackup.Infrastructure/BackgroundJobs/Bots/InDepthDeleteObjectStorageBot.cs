using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InDepthDeleteObjectStorageBot> _logger;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(InDepthDeleteObjectStorageBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public InDepthDeleteObjectStorageBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _scopeFactory = scopeFactory;
            //Logger
            using IServiceScope scope = _scopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<InDepthDeleteObjectStorageBot>>();
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                _logger.LogInformation("deleting uploaded file from ObjectStorage: {Path}, Id: {Id}", _backupRecord.Path, _contentDeliveryRecord.Id);
                //proceed
                await Task.Delay(Random.Shared.Next(1000), cancellationToken);
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
                _logger.LogInformation("Successfully deleted file from ObjectStorage: {Path}", _backupRecord.Path);
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                _logger.LogError(ex.Message);
                stopwatch.Stop();
            }
        }
    }
}
