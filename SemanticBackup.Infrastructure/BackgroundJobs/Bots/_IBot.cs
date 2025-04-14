using SemanticBackup.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    public interface IBot
    {
        string BotId { get; }
        string ResourceGroupId { get; }
        DateTime DateCreatedUtc { get; }
        Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken);
        BotStatus Status { get; }
    }

    public class BackupRecordDeliveryFeed
    {
        public DeliveryFeedType DeliveryFeedType { get; set; } = DeliveryFeedType.BackupDeliveryNotify;
        public long BackupRecordId { get; set; } = 0;
        public string BackupRecordDeliveryId { get; set; }
        public BackupRecordStatus Status { get; set; }
        public string Message { get; set; }
        public string NewFilePath { get; set; } = null;
        public long ElapsedMilliseconds { get; set; } = 0;
    }

    public enum DeliveryFeedType
    {
        BackupNotify,
        BackupDeliveryNotify,
    }

    public enum BotStatus
    {
        NotReady,
        Starting,
        Running,
        Completed,
        Error
    }
}
