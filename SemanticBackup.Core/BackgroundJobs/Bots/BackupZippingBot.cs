using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class BackupZippingRobot : IBot
    {
        private readonly BackupRecord _backupRecord;
        private readonly IBackupRecordPersistanceService _persistanceService;
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public BackupZippingRobot(BackupRecord backupRecord, IBackupRecordPersistanceService persistanceService, SharedTimeZone sharedTimeZone, ILogger logger)
        {
            this._backupRecord = backupRecord;
            this._persistanceService = persistanceService;
            this._sharedTimeZone = sharedTimeZone;
            this._logger = logger;
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
                DateTime currentTime = _sharedTimeZone.Now;
                using (ZipOutputStream s = new ZipOutputStream(File.Create(newZIPPath)))
                {

                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[4096];
                    var entry = new ZipEntry(Path.GetFileName(_backupRecord.Path));
                    entry.DateTime = currentTime;
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
                UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.READY.ToString(), "Successfull & Ready", stopwatch.ElapsedMilliseconds);
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

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {
                DateTime currentTime = _sharedTimeZone.Now;
                _persistanceService.UpdateStatusFeed(recordId, status, currentTime, message, elapsed);
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
