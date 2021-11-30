using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal interface IBot
    {
        Task RunAsync();
        bool IsCompleted { get; }
        bool IsStarted { get; }
    }
}
