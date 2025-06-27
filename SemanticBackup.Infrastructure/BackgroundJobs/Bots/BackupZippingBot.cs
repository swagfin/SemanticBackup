using ICSharpCode.SharpZipLib.Zip;
using SemanticBackup.Core.Helpers;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class BackupZippingBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroupId}::{_backupRecord.Id}::{nameof(BackupZippingBot)}";
        public string ResourceGroupId => _resourceGroupId;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public BackupZippingBot(string resourceGroupId, BackupRecord backupRecord)
        {
            _resourceGroupId = resourceGroupId;
            _backupRecord = backupRecord;
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                Console.WriteLine($"creating zip of: {_backupRecord.Path}");
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or may have been deleted, Path: {_backupRecord.Path}");
                //proceed
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
                await TryDeleteOldFileAsync(_backupRecord.Path);
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = null,
                    Status = BackupRecordStatus.READY,
                    Message = "Successfull & Ready",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    NewFilePath = newZIPPath,
                }, cancellationToken);
                Console.WriteLine($"successfully zipped file: {_backupRecord.Path}");
                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine(ex.Message);
                stopwatch.Stop();
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = null,
                    Status = BackupRecordStatus.ERROR,
                    Message = ex.Message,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                }, cancellationToken);
            }
        }

        private async Task TryDeleteOldFileAsync(string path)
        {
            try
            {
                await WithRetry.TaskAsync(() =>
                {
                    //check file exists
                    if (File.Exists(path))
                        File.Delete(path);
                    return Task.CompletedTask;

                }, 3, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex) { Console.WriteLine($"Failed to remove File after compression: {ex.Message}"); }
        }
    }
}
