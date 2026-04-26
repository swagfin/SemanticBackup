using Microsoft.Extensions.Logging.Abstractions;
using SemanticBackup.Core;
using SemanticBackup.Infrastructure.BackgroundJobs;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System.Collections.Concurrent;

namespace SemanticBackup.IntegrationTests
{
    [Collection(BotsManagerBackgroundJobIntegrationTestsCollection.CollectionName)]
    public class BotsManagerBackgroundJobIntegrationTests
    {
        [Fact]
        public async Task BotsManager_ShouldNotExceedGlobalMaxWorkers()
        {
            SystemConfigOptions systemConfigOptions = new SystemConfigOptions
            {
                MaxWorkers = 2
            };
            BotsManagerBackgroundJob manager = new BotsManagerBackgroundJob(NullLogger<BotsManagerBackgroundJob>.Instance, systemConfigOptions, null!, null!);
            await manager.StartAsync(CancellationToken.None);

            BotExecutionTracker tracker = new BotExecutionTracker();
            List<IBot> bots = new List<IBot>
            {
                new TrackingBot("bot-1", "rg-1", DateTime.UtcNow.AddMilliseconds(1), tracker, 700),
                new TrackingBot("bot-2", "rg-1", DateTime.UtcNow.AddMilliseconds(2), tracker, 700),
                new TrackingBot("bot-3", "rg-1", DateTime.UtcNow.AddMilliseconds(3), tracker, 700),
                new TrackingBot("bot-4", "rg-1", DateTime.UtcNow.AddMilliseconds(4), tracker, 700),
                new TrackingBot("bot-5", "rg-1", DateTime.UtcNow.AddMilliseconds(5), tracker, 700)
            };

            manager.AddBot(bots);

            bool allCompleted = await WaitForConditionAsync(() => tracker.CompletedCount == 5, TimeSpan.FromSeconds(30));
            Assert.True(allCompleted);
            Assert.True(tracker.MaxConcurrent <= 2);
        }

        [Fact]
        public async Task BotsManager_ShouldStartPendingBotsInFifoOrder_WhenSingleWorker()
        {
            SystemConfigOptions systemConfigOptions = new SystemConfigOptions
            {
                MaxWorkers = 1
            };
            BotsManagerBackgroundJob manager = new BotsManagerBackgroundJob(NullLogger<BotsManagerBackgroundJob>.Instance, systemConfigOptions, null!, null!);
            await manager.StartAsync(CancellationToken.None);

            BotExecutionTracker tracker = new BotExecutionTracker();
            DateTime now = DateTime.UtcNow;
            List<IBot> bots = new List<IBot>
            {
                new TrackingBot("bot-a", "rg-1", now.AddMilliseconds(1), tracker, 300),
                new TrackingBot("bot-b", "rg-1", now.AddMilliseconds(2), tracker, 300),
                new TrackingBot("bot-c", "rg-1", now.AddMilliseconds(3), tracker, 300),
                new TrackingBot("bot-d", "rg-1", now.AddMilliseconds(4), tracker, 300)
            };

            manager.AddBot(bots);

            bool allCompleted = await WaitForConditionAsync(() => tracker.CompletedCount == 4, TimeSpan.FromSeconds(30));
            Assert.True(allCompleted);

            List<string> expectedOrder = new List<string> { "bot-a", "bot-b", "bot-c", "bot-d" };
            List<string> actualOrder = tracker.StartOrder.ToList();
            Assert.Equal(expectedOrder, actualOrder);
            Assert.True(tracker.MaxConcurrent <= 1);
        }

        private static async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
        {
            DateTime expiresAt = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow <= expiresAt)
            {
                if (condition())
                    return true;
                await Task.Delay(100);
            }
            return false;
        }

        private class BotExecutionTracker
        {
            private int _runningCount = 0;
            private int _maxConcurrent = 0;
            private int _completedCount = 0;
            public ConcurrentQueue<string> StartOrder { get; } = new ConcurrentQueue<string>();
            public int CompletedCount => _completedCount;
            public int MaxConcurrent => _maxConcurrent;

            public void OnStart(string botId)
            {
                StartOrder.Enqueue(botId);
                int runningNow = Interlocked.Increment(ref _runningCount);
                UpdateMaxConcurrent(runningNow);
            }

            public void OnFinish()
            {
                _ = Interlocked.Decrement(ref _runningCount);
                _ = Interlocked.Increment(ref _completedCount);
            }

            private void UpdateMaxConcurrent(int runningNow)
            {
                while (true)
                {
                    int observedMax = _maxConcurrent;
                    if (runningNow <= observedMax)
                        break;
                    int original = Interlocked.CompareExchange(ref _maxConcurrent, runningNow, observedMax);
                    if (original == observedMax)
                        break;
                }
            }
        }

        private class TrackingBot : IBot
        {
            private readonly string _id;
            private readonly string _resourceGroupId;
            private readonly BotExecutionTracker _tracker;
            private readonly int _runDurationMs;
            public DateTime DateCreatedUtc { get; }
            public string BotId => _id;
            public string ResourceGroupId => _resourceGroupId;
            public BotStatus Status { get; private set; } = BotStatus.PendingStart;

            public TrackingBot(string id, string resourceGroupId, DateTime dateCreatedUtc, BotExecutionTracker tracker, int runDurationMs)
            {
                _id = id;
                _resourceGroupId = resourceGroupId;
                DateCreatedUtc = dateCreatedUtc;
                _tracker = tracker;
                _runDurationMs = runDurationMs;
            }

            public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
            {
                Status = BotStatus.Starting;
                _tracker.OnStart(BotId);
                Status = BotStatus.Running;
                try
                {
                    await Task.Delay(_runDurationMs, cancellationToken);
                    Status = BotStatus.Completed;
                }
                catch (OperationCanceledException)
                {
                    Status = BotStatus.Error;
                }
                finally
                {
                    _tracker.OnFinish();
                }
            }
        }
    }

    [CollectionDefinition(CollectionName, DisableParallelization = true)]
    public class BotsManagerBackgroundJobIntegrationTestsCollection
    {
        public const string CollectionName = "bots-manager-integration-tests";
    }
}
