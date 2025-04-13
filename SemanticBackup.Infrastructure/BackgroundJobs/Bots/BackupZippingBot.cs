using ICSharpCode.SharpZipLib.Zip;
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
    internal class BackupZippingRobot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupZippingRobot> _logger;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroupId}::{_backupRecord.Id}::{nameof(BackupZippingRobot)}";
        public string ResourceGroupId => _resourceGroupId;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public BackupZippingRobot(string resourceGroupId, BackupRecord backupRecord, IServiceScopeFactory scopeFactory)
        {
            _resourceGroupId = resourceGroupId;
            _backupRecord = backupRecord;
            _scopeFactory = scopeFactory;
            using IServiceScope scope = _scopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<BackupZippingRobot>>();
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                _logger.LogInformation("creating zip of: {Path}", _backupRecord.Path);
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or may have been deleted, Path: {_backupRecord.Path}");
                //proceed
                await Task.Delay(Random.Shared.Next(1000), cancellationToken);
                stopwatch.Start();
                Status = BotStatus.Running;
                //proceed
                string newZIPPath = _backupRecord.Path.Replace(".bak", ".zip");
                using (ZipOutputStream s = new(File.Create(newZIPPath)))
                {

                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[4096];
                    ZipEntry entry = new(Path.GetFileName(_backupRecord.Path))
                    {
                        DateTime = DateTime.UtcNow
                    };
                    s.PutNextEntry(entry);
                    using (FileStream fs = File.OpenRead(_backupRecord.Path))
                    {
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            s.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                    s.Finish();
                    s.Close();
                }
                stopwatch.Stop();
                TryDeleteOldFile(_backupRecord.Path);
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    BotId = BotId,
                    BackupRecordDeliveryId = _backupRecord.Id,
                    Status = BackupRecordStatus.READY,
                    Message = "Successfull & Ready",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    NewFilePath = newZIPPath,
                }, cancellationToken);
                _logger.LogInformation("successfully zipped file: {Path}", _backupRecord.Path);
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    BotId = BotId,
                    BackupRecordDeliveryId = _backupRecord.Id.ToString(),
                    Status = BackupRecordStatus.ERROR,
                    Message = ex.Message,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                }, cancellationToken);
            }
        }

        private void TryDeleteOldFile(string path)
        {
            try
            {
                bool success = false;
                int attempts = 0;
                do
                {
                    try
                    {
                        attempts++;
                        if (File.Exists(path))
                            File.Delete(path);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        if (attempts >= 10)
                        {
                            Thread.Sleep(2000);
                            throw new Exception($"Maximum Deletion Attempts, Error: {ex.Message}");
                        }
                    }
                }
                while (!success);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning("The File Name Failed to Delete, Error: {Message}, File: {Path}", ex.Message, path);
            }
        }
    }
}
