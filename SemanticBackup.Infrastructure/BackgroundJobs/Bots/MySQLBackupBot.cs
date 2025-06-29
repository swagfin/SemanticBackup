using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class MySQLBackupBot : IBot
    {
        private readonly ResourceGroup _resourceGroup;
        private readonly string _databaseName;
        private readonly BackupRecord _backupRecord;
        private readonly IBackupProviderForMySQLServer _providerForMySQLServer;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(MySQLBackupBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public MySQLBackupBot(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord, IBackupProviderForMySQLServer providerForMySQLServer)
        {
            _databaseName = databaseName;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
            _providerForMySQLServer = providerForMySQLServer;
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
                //Execute Service
                bool backupedUp = await _providerForMySQLServer.BackupDatabaseAsync(_databaseName, _resourceGroup, _backupRecord);
                stopwatch.Stop();
                if (!backupedUp)
                    throw new Exception("Creating Backup Failed to Return Success Completion");
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
                Console.WriteLine(ex.Message);
                Status = BotStatus.Error;
                stopwatch.Stop();
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
