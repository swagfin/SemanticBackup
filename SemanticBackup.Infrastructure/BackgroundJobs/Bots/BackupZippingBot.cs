using ICSharpCode.SharpZipLib.Zip;
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
    internal class BackupZippingRobot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupZippingRobot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;

        public string BotId => _backupRecord.Id.ToString();

        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        public BackupZippingRobot(string resourceGroupId, BackupRecord backupRecord, IServiceScopeFactory scopeFactory)
        {
            this._resourceGroupId = resourceGroupId;
            this._backupRecord = backupRecord;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<BackupZippingRobot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Creating Zip of Db: {_backupRecord.Path}");
                CheckIfFileExistsOrRemove(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();

                string newZIPPath = _backupRecord.Path.Replace(".bak", ".zip");
                using (ZipOutputStream s = new ZipOutputStream(File.Create(newZIPPath)))
                {

                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[4096];
                    var entry = new ZipEntry(Path.GetFileName(_backupRecord.Path));
                    entry.DateTime = DateTime.UtcNow;
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
                UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.READY.ToString(), "Successfull & Ready", stopwatch.ElapsedMilliseconds, newZIPPath);
                _logger.LogInformation($"Creating Zip of Db: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private void CheckIfFileExistsOrRemove(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"No Database File In Path or May have been deleted, Path: {path}");
        }

        private void UpdateBackupFeed(long recordId, string status, string message, long elapsed, string newZIPPath = null)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                    _persistanceService.UpdateStatusFeedAsync(recordId, status, message, elapsed, newZIPPath);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error Updating Feed: " + ex.Message);
            }
            finally
            {
                IsCompleted = true;
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
            catch (Exception ex) { this._logger.LogWarning($"The File Name Failed to Delete,Error: {ex.Message}, File: {path}"); }
        }
    }
}
