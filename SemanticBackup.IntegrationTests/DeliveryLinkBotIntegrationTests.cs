using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.IntegrationTests
{
    public class DeliveryLinkBotIntegrationTests
    {
        [Fact]
        public async Task UploaderLinkGenBot_ShouldGenerateShortAndLongLinks()
        {
            ResourceGroup shortResourceGroup = new ResourceGroup
            {
                Id = "rg-short",
                Name = "Short Link Group",
                DbType = DbTypes.SQLSERVER2019.ToString(),
                DbServer = "n/a",
                DbUsername = "n/a",
                BackupDeliveryConfig = new BackupDeliveryConfig
                {
                    DownloadLink = new DownloadLinkDeliveryConfig
                    {
                        IsEnabled = true,
                        UseShortDownloadLink = true
                    }
                }
            };
            BackupRecord backupRecord = new BackupRecord
            {
                Id = 2222,
                BackupDatabaseInfoId = "db-link",
                Path = "unused"
            };
            BackupRecordDelivery shortDelivery = new BackupRecordDelivery
            {
                Id = "short-delivery",
                BackupRecordId = backupRecord.Id,
                DeliveryType = BackupDeliveryConfigTypes.DownloadLink.ToString()
            };
            UploaderLinkGenBot shortBot = new UploaderLinkGenBot(shortResourceGroup, backupRecord, shortDelivery);
            BackupRecordDeliveryFeed? shortFeed = null;

            await shortBot.RunAsync((feed, token) =>
            {
                shortFeed = feed;
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(BotStatus.Completed, shortBot.Status);
            Assert.NotNull(shortFeed);
            Assert.Equal(BackupRecordStatus.READY, shortFeed.Status);
            Assert.False(string.IsNullOrWhiteSpace(shortFeed.Message));

            ResourceGroup longResourceGroup = new ResourceGroup
            {
                Id = "rg-long",
                Name = "Long Link Group",
                DbType = DbTypes.SQLSERVER2019.ToString(),
                DbServer = "n/a",
                DbUsername = "n/a",
                BackupDeliveryConfig = new BackupDeliveryConfig
                {
                    DownloadLink = new DownloadLinkDeliveryConfig
                    {
                        IsEnabled = true,
                        UseShortDownloadLink = false
                    }
                }
            };
            BackupRecordDelivery longDelivery = new BackupRecordDelivery
            {
                Id = "long-delivery",
                BackupRecordId = backupRecord.Id,
                DeliveryType = BackupDeliveryConfigTypes.DownloadLink.ToString()
            };
            UploaderLinkGenBot longBot = new UploaderLinkGenBot(longResourceGroup, backupRecord, longDelivery);
            BackupRecordDeliveryFeed? longFeed = null;

            await longBot.RunAsync((feed, token) =>
            {
                longFeed = feed;
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.Equal(BotStatus.Completed, longBot.Status);
            Assert.NotNull(longFeed);
            Assert.Equal(BackupRecordStatus.READY, longFeed.Status);
            Assert.Contains("?token=", longFeed.Message, StringComparison.Ordinal);
        }
    }
}
