using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    public interface IBot
    {
        Task RunAsync();
        bool IsCompleted { get; }
        bool IsStarted { get; }
        string ResourceGroupId { get; }
    }
}
