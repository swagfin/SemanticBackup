using Dropbox.Api;
using Dropbox.Api.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderDropboxBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderDropboxBot> _logger;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(UploaderDropboxBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public UploaderDropboxBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _scopeFactory = scopeFactory;
            //Logger
            using IServiceScope scope = _scopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<UploaderDropboxBot>>();
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                _logger.LogInformation("uploading file to Dropbox: {Path}", _backupRecord.Path);
                await Task.Delay(Random.Shared.Next(1000), cancellationToken);
                DropboxDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Dropbox ?? throw new Exception("no valid dropbox config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //check path
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or May have been deleted, Path: {_backupRecord.Path}");
                //FTP Upload
                string executionMessage = "Dropbox Uploading...";
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
                using (MemoryStream mem = new(await File.ReadAllBytesAsync(this._backupRecord.Path, cancellationToken)))
                {
                    FileMetadata updated = await dbx.Files.UploadAsync(string.Format("{0}{1}", validDirectory, fileName), WriteMode.Overwrite.Instance, body: mem);
                    executionMessage = $"Uploaded to: {validDirectory}";
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
                _logger.LogInformation("Successfully uploaded file to Dropbox: {Path}", _backupRecord.Path);
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                _logger.LogError(ex.Message);
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

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IContentDeliveryRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                _persistanceService.UpdateStatusFeedAsync(recordId, status, message, elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Updating Feed: {Message}", ex.Message);
            }
        }
    }
}
