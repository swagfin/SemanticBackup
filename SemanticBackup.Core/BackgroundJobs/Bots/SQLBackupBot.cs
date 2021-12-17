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
    internal class SQLBackupBot : IBot
    {
        private readonly string _resourceGroup;
        private readonly BackupDatabaseInfo _databaseInfo;
        private readonly BackupRecord _backupRecord;
        private readonly ISQLServerBackupProviderService _backupProviderService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup;
        public string BotId => _backupRecord.Id;
        public SQLBackupBot(string resourceGroupId, BackupDatabaseInfo databaseInfo, BackupRecord backupRecord, ISQLServerBackupProviderService backupProviderService, IServiceScopeFactory scopeFactory, ILogger logger)
        {
            this._resourceGroup = resourceGroupId;
            this._databaseInfo = databaseInfo;
            this._backupRecord = backupRecord;
            this._backupProviderService = backupProviderService;
            this._scopeFactory = scopeFactory;
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
                bool backupedUp = await _backupProviderService.BackupDatabaseAsync(_databaseInfo, _backupRecord);
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
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupRecordPersistanceService _persistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordPersistanceService>();
                    _persistanceService.UpdateStatusFeed(recordId, status, message, elapsed);
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
