using Minio;
using Minio.DataModel.Args;
using SemanticBackup.Core.Helpers;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
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
        public BotStatus Status { get; internal set; } = BotStatus.PendingStart;

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
                //proceed
                await WithRetry.TaskAsync(async () =>
                {
                    //Container
                    string validBucket = string.IsNullOrWhiteSpace(settings.Bucket) ? "backups" : settings.Bucket;
                    //Filename
                    string fileName = Path.GetFileName(this._backupRecord.Path);
                    //Proceed
                    using IMinioClient minioClient = new MinioClient()
                                                    .WithEndpoint(settings.Server, settings.Port)
                                                    .WithCredentials(settings.AccessKey, settings.SecretKey)
                                                    .WithSSL(settings.UseSsl)
                                                    .Build();
                    //deleting recursive
                    List<Minio.DataModel.Item> objectVersions = [];
                    // Attempt to list object versions
                    ListObjectsArgs listArgs = new ListObjectsArgs()
                                              .WithBucket(validBucket)
                                              .WithPrefix(fileName)
                                              .WithRecursive(true)
                                              .WithVersions(true);

                    await foreach (Minio.DataModel.Item version in minioClient.ListObjectsEnumAsync(listArgs))
                    {
                        if (version.Key == fileName && !string.IsNullOrEmpty(version.VersionId))
                        {
                            objectVersions.Add(version);
                        }
                    }

                    if (objectVersions.Count > 0)
                    {
                        // Bucket is versioned: delete all versions
                        Console.WriteLine($"Found {objectVersions.Count} versions. Deleting all...");

                        foreach (Minio.DataModel.Item version in objectVersions)
                        {
                            await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                                             .WithBucket(validBucket)
                                             .WithObject(fileName)
                                             .WithVersionId(version.VersionId), cancellationToken);

                            Console.WriteLine($"Deleted version {version.VersionId}");
                        }
                    }
                    else
                    {
                        // Bucket is non-versioned or no versions found: delete normally
                        Console.WriteLine("No versions found. Deleting object directly...");

                        await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                                         .WithBucket(validBucket)
                                         .WithObject(fileName), cancellationToken);
                    }

                }, maxRetries: 2, delay: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);


                stopwatch.Stop();

                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine($"[Error] {nameof(InDepthDeleteObjectStorageBot)}: {ex.Message}");
                stopwatch.Stop();
            }
        }
    }
}
