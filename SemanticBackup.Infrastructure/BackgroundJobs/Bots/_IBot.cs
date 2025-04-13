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
        public string BotId { get; set; }
        public string BackupRecordDeliveryId { get; set; }
        public BackupRecordStatus Status { get; set; }
        public string Message { get; set; }
        public string NewFilePath { get; set; } = null;
        public long ElapsedMilliseconds { get; set; } = 0;
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
