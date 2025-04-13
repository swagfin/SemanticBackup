using Dropbox.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InDepthDeleteDropboxBot> _logger;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(InDepthDeleteDropboxBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public InDepthDeleteDropboxBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _scopeFactory = scopeFactory;
            //Logger
            using IServiceScope scope = _scopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<InDepthDeleteDropboxBot>>();
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                _logger.LogInformation("Deleting backup file from DropBox: {Path}, Id: {Id}", this._backupRecord.Path, _contentDeliveryRecord.Id);
                //proceed
                await Task.Delay(Random.Shared.Next(1000), cancellationToken);
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
                _logger.LogInformation("Successfully deleted Backup File From DropBox: {Path}", _backupRecord.Path);
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                this._logger.LogWarning(ex.Message);
                stopwatch.Stop();
            }
        }
    }
}
