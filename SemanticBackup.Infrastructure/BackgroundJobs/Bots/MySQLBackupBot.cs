using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class MySQLBackupBot : IBot
    {
        private readonly ResourceGroup _resourceGroup;
        private readonly string _databaseName;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MySQLBackupBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _backupRecord.Id.ToString();
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public MySQLBackupBot(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord, IServiceScopeFactory serviceScopeFactory)
        {
            this._databaseName = databaseName;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
            this._scopeFactory = serviceScopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<MySQLBackupBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Creating Backup of Db: {_databaseName}");
                EnsureFolderExists(_backupRecord.Path);
                await Task.Delay(new Random().Next(1000));
                stopwatch.Start();
                //Execute Service
                bool backupedUp = false;
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupProviderForMySQLServer providerService = scope.ServiceProvider.GetRequiredService<IBackupProviderForMySQLServer>();
                    backupedUp = await providerService.BackupDatabaseAsync(_databaseName, _resourceGroup, _backupRecord);
                }
                stopwatch.Stop();
                if (backupedUp)
                    UpdateBackupFeed(_backupRecord.Id, BackupRecordBackupStatus.COMPLETED.ToString(), "Successfull", stopwatch.ElapsedMilliseconds);
                else
                    throw new Exception("Creating Backup Failed to Return Success Completion");
                _logger.LogInformation($"Creating Backup of Db: {_databaseName}...SUCCESS");
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

        private void UpdateBackupFeed(long recordId, string status, string message, long elapsed)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    IBackupRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                    _persistanceService.UpdateStatusFeedAsync(recordId, status, message, elapsed);
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
