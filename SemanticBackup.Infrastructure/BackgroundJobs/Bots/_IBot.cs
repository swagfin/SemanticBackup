using System;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    public interface IBot
    {
        Task RunAsync();
        bool IsCompleted { get; }
        bool IsStarted { get; }
        string ResourceGroupId { get; }
        string BotId { get; }
        DateTime DateCreatedUtc { get; }
    }
}
