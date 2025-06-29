using SemanticBackup.Core.Helpers;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class SQLBackupBot : IBot
    {
        private readonly ResourceGroup _resourceGroup;
        private readonly string _databaseName;
        private readonly BackupRecord _backupRecord;
        private readonly IBackupProviderForSQLServer _providerForSQLServer;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(SQLBackupBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.PendingStart;

        public SQLBackupBot(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord, IBackupProviderForSQLServer providerForSQLServer)
        {
            _databaseName = databaseName;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _providerForSQLServer = providerForSQLServer;
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                Console.WriteLine($"creating backup of Db: {_databaseName}");
                string directory = Path.GetDirectoryName(_backupRecord.Path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                //proceed
                stopwatch.Start();
                Status = BotStatus.Running;
                //proceed
                await WithRetry.TaskAsync(async () =>
                {

                    //Execute Service
                    if (!await _providerForSQLServer.BackupDatabaseAsync(_databaseName, _resourceGroup, _backupRecord))
                        throw new Exception("Creating Backup Failed to Return Success Completion");

                }, maxRetries: 2, delay: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);

                stopwatch.Stop();
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = null,
                    Status = BackupRecordStatus.COMPLETED,
                    Message = "Successfull",
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);

                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine($"[Error] {nameof(SQLBackupBot)}: {ex.Message}");
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = null,
                    Status = BackupRecordStatus.ERROR,
                    Message = ex.Message,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);
            }
        }
    }
}
