using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class SQLRestoreBot : IBot
    {
        private readonly ResourceGroup _resourceGroup;
        private readonly string _databaseName;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SQLRestoreBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _backupRecord.Id.ToString();
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public SQLRestoreBot(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord, IServiceScopeFactory scopeFactory)
        {
            this._databaseName = databaseName;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<SQLRestoreBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Begining RESTORE of Db: {_databaseName}");
                EnsureBackupFileExists(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();
                //Execute Service
                bool restoredSuccess = false;
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupProviderForSQLServer backupProviderService = scope.ServiceProvider.GetRequiredService<IBackupProviderForSQLServer>();
                    restoredSuccess = await backupProviderService.RestoreDatabaseAsync(_databaseName, _resourceGroup, _backupRecord);
                }
                stopwatch.Stop();
                if (restoredSuccess)
                    UpdateRestoreStatusFeed(_backupRecord.Id, BackupRecordRestoreStatus.RESTORE_COMPLETED.ToString(), "Restored Successfully");
                else
                    throw new Exception("RESTORE Failed to Return Success Completion");
                _logger.LogInformation($"RESTORE of Db: {_databaseName}...SUCCESS, Completion Time: {stopwatch.ElapsedMilliseconds:N0} Milliseconds");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateRestoreStatusFeed(_backupRecord.Id, BackupRecordRestoreStatus.FAILED_RESTORE.ToString(), ex.Message);
            }
        }

        private void EnsureBackupFileExists(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"No Database File In Path or May have been deleted, Path: {path}");
        }

        private void UpdateRestoreStatusFeed(long recordId, string status, string message)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                    _persistanceService.UpdateRestoreStatusFeedAsync(recordId, status, message);
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
    }
}
