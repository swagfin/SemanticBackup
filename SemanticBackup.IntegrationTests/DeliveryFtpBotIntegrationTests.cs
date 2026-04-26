using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using SemanticBackup.IntegrationTests.Helpers;
using System.Net;

namespace SemanticBackup.IntegrationTests
{
    [Collection(SemanticBackupIntegrationCollection.CollectionName)]
    public class DeliveryFtpBotIntegrationTests
    {
        private readonly FtpContainerFixture _ftpFixture;

        public DeliveryFtpBotIntegrationTests(FtpContainerFixture ftpFixture)
        {
            _ftpFixture = ftpFixture;
        }

        [Fact]
        public async Task UploaderFTPBot_ShouldUploadBackupFileToFtpServer()
        {
            if (!_ftpFixture.IsDockerAvailable)
                return;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"sb-ftp-{Guid.NewGuid():N}.bak");
            string fileContents = "semantic-backup-ftp-test";
            await File.WriteAllTextAsync(tempFilePath, fileContents);

            try
            {
                ResourceGroup resourceGroup = new ResourceGroup
                {
                    Id = "rg-ftp",
                    Name = "FTP Group",
                    DbType = DbTypes.SQLSERVER2019.ToString(),
                    DbServer = "n/a",
                    DbUsername = "n/a",
                    BackupDeliveryConfig = new BackupDeliveryConfig
                    {
                        Ftp = new FtpDeliveryConfig
                        {
                            IsEnabled = true,
                            Server = _ftpFixture.Server,
                            Username = _ftpFixture.Username,
                            Password = _ftpFixture.Password,
                            Directory = "/"
                        }
                    }
                };
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 4444,
                    BackupDatabaseInfoId = "db-ftp",
                    Path = tempFilePath
                };
                BackupRecordDelivery backupRecordDelivery = new BackupRecordDelivery
                {
                    Id = "ftp-delivery",
                    BackupRecordId = backupRecord.Id,
                    DeliveryType = BackupDeliveryConfigTypes.Ftp.ToString()
                };
                UploaderFTPBot bot = new UploaderFTPBot(resourceGroup, backupRecord, backupRecordDelivery);
                BackupRecordDeliveryFeed? ftpFeed = null;

                await bot.RunAsync((feed, token) =>
                {
                    ftpFeed = feed;
                    return Task.CompletedTask;
                }, CancellationToken.None);

                Assert.Equal(BotStatus.Completed, bot.Status);
                Assert.NotNull(ftpFeed);
                Assert.Equal(BackupRecordStatus.READY, ftpFeed.Status);
                Assert.Contains("Uploaded to Server", ftpFeed.Message, StringComparison.Ordinal);

                string uploadedFileName = Path.GetFileName(tempFilePath);
                string downloadUrl = $"{_ftpFixture.Server}/{uploadedFileName}";
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(downloadUrl);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_ftpFixture.Username, _ftpFixture.Password);
                request.UsePassive = true;
                request.UseBinary = true;
                using FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                using Stream responseStream = response.GetResponseStream();
                using StreamReader reader = new StreamReader(responseStream);
                string downloadedContents = await reader.ReadToEndAsync();
                Assert.Equal(fileContents, downloadedContents);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}
