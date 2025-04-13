using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderFTPBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderFTPBot> _logger;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(UploaderFTPBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public UploaderFTPBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _scopeFactory = scopeFactory;
            //Logger
            using IServiceScope scope = _scopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<UploaderFTPBot>>();
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                _logger.LogInformation("uploading file to FTP Server: {Path}", _backupRecord.Path);
                await Task.Delay(Random.Shared.Next(1000), cancellationToken);
                FtpDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Ftp ?? throw new Exception("no valid ftp config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //check file
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or May have been deleted, Path: {_backupRecord.Path}");
                //FTP Upload
                string executionMessage = "FTP Uploading...";
                //Directory
                string validDirectory = (string.IsNullOrWhiteSpace(settings.Directory)) ? "/" : settings.Directory;
                validDirectory = validDirectory.EndsWith('/') ? validDirectory : validDirectory + "/";
                validDirectory = validDirectory.StartsWith('/') ? validDirectory : "/" + validDirectory;
                string validServerName = settings.Server.Replace("ftp", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty);
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                try
                {
                    string fullServerUrl = string.Format("ftp://{0}{1}{2}", validServerName, validDirectory, fileName);
                    byte[] fileContents;
                    using (FileStream sourceStream = File.OpenRead(this._backupRecord.Path))
                    {
                        fileContents = new byte[sourceStream.Length];
                        await sourceStream.ReadAsync(fileContents, cancellationToken);
                    }
                    using HttpClient client = new();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}")));
                    ByteArrayContent content = new(fileContents);
                    HttpResponseMessage response = await client.PutAsync(fullServerUrl, content, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        executionMessage = $"Uploaded to Server: {settings.Server}";
                    }
                    else
                    {
                        throw new Exception($"Failed to upload. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
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

                _logger.LogInformation("Successfully uploaded file to FTP Server: {Path}", _backupRecord.Path);
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
    }
}
