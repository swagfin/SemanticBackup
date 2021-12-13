using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class MySQLBackupBot : IBot
    {
        private readonly string _resourceGroup;
        private readonly BackupDatabaseInfo _databaseInfo;
        private readonly BackupRecord _backupRecord;
        private readonly IMySQLServerBackupProviderService _backupProviderService;
        private readonly IBackupRecordPersistanceService _persistanceService;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string resourceGroupId => _resourceGroup;

        public MySQLBackupBot(string resourceGroupId, BackupDatabaseInfo databaseInfo, BackupRecord backupRecord, IMySQLServerBackupProviderService backupProviderService, IBackupRecordPersistanceService persistanceService, ILogger logger)
        {
            this._resourceGroup = resourceGroupId;
            this._databaseInfo = databaseInfo;
            this._backupRecord = backupRecord;
            this._backupProviderService = backupProviderService;
            this._persistanceService = persistanceService;
            this._logger = logger;
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Creating Backup of Db: {_databaseInfo.DatabaseName}");
                EnsureFolderExists(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();
                //Execute Service
                bool backupedUp = _backupProviderService.BackupDatabase(_databaseInfo, _backupRecord);
                stopwatch.Stop();
                if (backupedUp)
                    UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.COMPLETED.ToString(), "Successfull", stopwatch.ElapsedMilliseconds);
                else
                    throw new Exception("Creating Backup Failed to Return Success Completion");
                _logger.LogInformation($"Creating Backup of Db: {_databaseInfo.DatabaseName}...SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private void EnsureFolderExists(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {
                _persistanceService.UpdateStatusFeed(recordId, status, message, elapsed);
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
    }
}
