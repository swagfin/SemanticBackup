using Minio;
using Minio.DataModel.Args;
using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using SemanticBackup.IntegrationTests.Helpers;

namespace SemanticBackup.IntegrationTests
{
    [Collection(SemanticBackupIntegrationCollection.CollectionName)]
    public class DeliveryObjectStorageBotIntegrationTests
    {
        private readonly MinioContainerFixture _minioFixture;

        public DeliveryObjectStorageBotIntegrationTests(MinioContainerFixture minioFixture)
        {
            _minioFixture = minioFixture;
        }

        [Fact]
        public async Task UploaderObjectStorageBot_ShouldUploadBackupFileToMinio()
        {
            if (!_minioFixture.IsDockerAvailable)
                return;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"sb-objectstorage-{Guid.NewGuid():N}.bak");
            string fileContents = "semantic-backup-object-storage-test";
            await File.WriteAllTextAsync(tempFilePath, fileContents);

            try
            {
                ResourceGroup resourceGroup = CreateResourceGroup();
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 3333,
                    BackupDatabaseInfoId = "db-object-storage",
                    Path = tempFilePath
                };
                BackupRecordDelivery backupRecordDelivery = new BackupRecordDelivery
                {
                    Id = "object-storage-delivery",
                    BackupRecordId = backupRecord.Id,
                    DeliveryType = BackupDeliveryConfigTypes.ObjectStorage.ToString()
                };
                UploaderObjectStorageBot bot = new UploaderObjectStorageBot(resourceGroup, backupRecord, backupRecordDelivery);
                BackupRecordDeliveryFeed? objectStorageFeed = null;

                await bot.RunAsync((feed, token) =>
                {
                    objectStorageFeed = feed;
                    return Task.CompletedTask;
                }, CancellationToken.None);

                Assert.Equal(BotStatus.Completed, bot.Status);
                Assert.NotNull(objectStorageFeed);
                Assert.Equal(BackupRecordStatus.READY, objectStorageFeed.Status);
                Assert.Contains("Uploaded to Bucket", objectStorageFeed.Message, StringComparison.Ordinal);

                string uploadedObjectName = Path.GetFileName(tempFilePath);
                bool objectExists = await ObjectExistsAsync(uploadedObjectName);
                Assert.True(objectExists);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [Fact]
        public async Task InDepthDeleteObjectStorageBot_ShouldDeleteUploadedObjectFromMinio()
        {
            if (!_minioFixture.IsDockerAvailable)
                return;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"sb-objectstorage-delete-{Guid.NewGuid():N}.bak");
            string fileContents = "semantic-backup-object-storage-delete-test";
            await File.WriteAllTextAsync(tempFilePath, fileContents);

            try
            {
                ResourceGroup resourceGroup = CreateResourceGroup();
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 3334,
                    BackupDatabaseInfoId = "db-object-storage-delete",
                    Path = tempFilePath
                };
                BackupRecordDelivery backupRecordDelivery = new BackupRecordDelivery
                {
                    Id = "object-storage-delete-delivery",
                    BackupRecordId = backupRecord.Id,
                    DeliveryType = BackupDeliveryConfigTypes.ObjectStorage.ToString()
                };

                UploaderObjectStorageBot uploaderBot = new UploaderObjectStorageBot(resourceGroup, backupRecord, backupRecordDelivery);
                await uploaderBot.RunAsync((feed, token) => Task.CompletedTask, CancellationToken.None);
                Assert.Equal(BotStatus.Completed, uploaderBot.Status);

                string uploadedObjectName = Path.GetFileName(tempFilePath);
                bool objectExistsBeforeDelete = await ObjectExistsAsync(uploadedObjectName);
                Assert.True(objectExistsBeforeDelete);

                InDepthDeleteObjectStorageBot deleteBot = new InDepthDeleteObjectStorageBot(resourceGroup, backupRecord, backupRecordDelivery);
                await deleteBot.RunAsync((feed, token) => Task.CompletedTask, CancellationToken.None);
                Assert.Equal(BotStatus.Completed, deleteBot.Status);

                bool objectExistsAfterDelete = await ObjectExistsAsync(uploadedObjectName);
                Assert.False(objectExistsAfterDelete);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        private ResourceGroup CreateResourceGroup()
        {
            return new ResourceGroup
            {
                Id = "rg-object-storage",
                Name = "Object Storage Group",
                DbType = DbTypes.SQLSERVER2019.ToString(),
                DbServer = "n/a",
                DbUsername = "n/a",
                BackupDeliveryConfig = new BackupDeliveryConfig
                {
                    ObjectStorage = new ObjectStorageDeliveryConfig
                    {
                        IsEnabled = true,
                        Server = _minioFixture.Server,
                        Port = _minioFixture.Port,
                        Bucket = _minioFixture.Bucket,
                        AccessKey = _minioFixture.AccessKey,
                        SecretKey = _minioFixture.SecretKey,
                        UseSsl = false
                    }
                }
            };
        }

        private async Task<bool> ObjectExistsAsync(string objectName)
        {
            try
            {
                using IMinioClient minioClient = new MinioClient().WithEndpoint(_minioFixture.Server, _minioFixture.Port).WithCredentials(_minioFixture.AccessKey, _minioFixture.SecretKey).WithSSL(false).Build();
                StatObjectArgs statObjectArgs = new StatObjectArgs().WithBucket(_minioFixture.Bucket).WithObject(objectName);
                _ = await minioClient.StatObjectAsync(statObjectArgs, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
