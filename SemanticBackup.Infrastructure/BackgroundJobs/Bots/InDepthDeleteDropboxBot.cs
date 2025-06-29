using Dropbox.Api;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class InDepthDeleteDropboxBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(InDepthDeleteDropboxBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.PendingStart;

        public InDepthDeleteDropboxBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord)
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
                Console.WriteLine($"Deleting backup file from DropBox: {_backupRecord.Path}, Id: {_contentDeliveryRecord.Id}");
                //proceed
                DropboxDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Dropbox ?? throw new Exception("no valid dropbox config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //Directory
                string validDirectory = string.IsNullOrWhiteSpace(settings.Directory) ? "/" : settings.Directory;
                validDirectory = validDirectory.EndsWith('/') ? validDirectory : validDirectory + "/";
                validDirectory = validDirectory.StartsWith('/') ? validDirectory : "/" + validDirectory;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                if (string.IsNullOrWhiteSpace(settings.AccessToken))
                    throw new Exception("Access Token is NULL");
                //Proceed
                using (DropboxClient dbx = new(settings.AccessToken.Trim()))
                {
                    string initialFileName = string.Format("{0}{1}", validDirectory, fileName);
                    Dropbox.Api.Files.DeleteResult delResponse = await dbx.Files.DeleteV2Async(initialFileName, null);
                }
                stopwatch.Stop();

                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine($"[Error] {nameof(InDepthDeleteDropboxBot)}: {ex.Message}");
                stopwatch.Stop();
            }
        }
    }
}
