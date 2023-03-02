using Microsoft.Extensions.DependencyInjection;
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
    internal class SQLRestoreBot : IBot
    {
        private readonly string _resourceGroup;
        private readonly BackupDatabaseInfo _databaseInfo;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SQLRestoreBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup;
        public string BotId => _backupRecord.Id;
        public SQLRestoreBot(string resourceGroupId, BackupDatabaseInfo databaseInfo, BackupRecord backupRecord, IServiceScopeFactory scopeFactory)
        {
            this._resourceGroup = resourceGroupId;
            this._databaseInfo = databaseInfo;
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
                _logger.LogInformation($"Begining RESTORE of Db: {_databaseInfo.DatabaseName}");
                EnsureBackupFileExists(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();
                //Execute Service
                bool restoredSuccess = false;
                using (var scope = _scopeFactory.CreateScope())
                {
                    ISQLServerBackupProviderService backupProviderService = scope.ServiceProvider.GetRequiredService<ISQLServerBackupProviderService>();
                    restoredSuccess = await backupProviderService.RestoreDatabaseAsync(_databaseInfo, _backupRecord);
                }
                stopwatch.Stop();
                if (restoredSuccess)
                    UpdateRestoreStatusFeed(_backupRecord.Id, BackupRecordRestoreStatus.RESTORE_COMPLETED.ToString(), "Restored Successfully");
                else
                    throw new Exception("RESTORE Failed to Return Success Completion");
                _logger.LogInformation($"RESTORE of Db: {_databaseInfo.DatabaseName}...SUCCESS, Completion Time: {stopwatch.ElapsedMilliseconds:N0} Milliseconds");
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

        private void UpdateRestoreStatusFeed(string recordId, string status, string message)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupRecordPersistanceService _persistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
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
