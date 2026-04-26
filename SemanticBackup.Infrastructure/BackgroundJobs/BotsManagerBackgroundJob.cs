using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BotsManagerBackgroundJob : IHostedService
    {
        private readonly ILogger<BotsManagerBackgroundJob> _logger;
        private readonly SystemConfigOptions _systemConfigOptions;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _deliveryRecordRepository;
        private readonly object _botsLock = new();
        private readonly HashSet<string> _startedBotIds = [];
        private List<IBot> Bots { get; set; } = [];

        public BotsManagerBackgroundJob(ILogger<BotsManagerBackgroundJob> logger, SystemConfigOptions systemConfigOptions, IBackupRecordRepository backupRecordRepository, IContentDeliveryRecordRepository deliveryRecordRepository)
        {
            _logger = logger;
            _systemConfigOptions = systemConfigOptions;
            _backupRecordRepository = backupRecordRepository;
            _deliveryRecordRepository = deliveryRecordRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupBotsBackgroundService(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void AddBot(IBot bot)
        {
            if (bot == null)
                return;
            lock (_botsLock)
            {
                bool hasExisting = Bots.Any(x => x.BotId == bot.BotId);
                if (!hasExisting)
                    Bots.Add(bot);
            }
        }

        public void AddBot(List<IBot> bots)
        {
            if (bots == null || bots.Count == 0)
                return;
            lock (_botsLock)
            {
                foreach (IBot bot in bots)
                {
                    if (bot == null)
                        continue;
                    bool hasExisting = Bots.Any(x => x.BotId == bot.BotId);
                    if (!hasExisting)
                        Bots.Add(bot);
                }
            }
        }

        public void TerminateBots(List<string> botIds)
        {
            if (botIds == null || botIds.Count == 0)
                return;
            lock (_botsLock)
            {
                List<IBot> botsToRemove = [.. Bots.Where(x => botIds.Contains(x.BotId))];
                foreach (IBot bot in botsToRemove)
                {
                    Bots.Remove(bot);
                    _startedBotIds.Remove(bot.BotId);
                }
            }
        }

        private void SetupBotsBackgroundService(CancellationToken cancellationToken)
        {
            int maxWorkers = _systemConfigOptions.MaxWorkers < 1 ? 1 : _systemConfigOptions.MaxWorkers;
            Thread t = new(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    try
                    {
                        List<IBot> botsToStart = [];
                        int runningBots = 0;
                        int pendingBots = 0;

                        lock (_botsLock)
                        {
                            RemoveFinishedBotsUnsafe();
                            runningBots = _startedBotIds.Count;
                            pendingBots = Bots.Count(x => x.Status == BotStatus.PendingStart && !_startedBotIds.Contains(x.BotId));
                            int availableWorkers = maxWorkers - runningBots;
                            if (availableWorkers > 0)
                            {
                                botsToStart = Bots.Where(x => x.Status == BotStatus.PendingStart && !_startedBotIds.Contains(x.BotId))
                                                  .OrderBy(x => x.DateCreatedUtc)
                                                  .Take(availableWorkers)
                                                  .ToList();
                                foreach (IBot bot in botsToStart)
                                    _startedBotIds.Add(bot.BotId);
                            }
                        }

                        foreach (IBot bot in botsToStart)
                        {
                            Debug.WriteLine($"Running bot #{bot.BotId}");
                            _ = ExecuteBotAsync(bot, cancellationToken);
                        }

                        if (pendingBots > 0 && runningBots >= maxWorkers)
                            Debug.WriteLine($"[{nameof(BotsManagerBackgroundJob)}] {runningBots}-Bot(s) are Busy, maximum workers configured to: {maxWorkers}, waiting for available workers....");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error: {Message}", ex.Message);
                    }
                }
            });
            t.Start();
        }

        private async Task ExecuteBotAsync(IBot bot, CancellationToken cancellationToken)
        {
            try
            {
                await bot.RunAsync(OnDeliveryFeedUpdate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Bot '{BotId}' failed unexpectedly: {Message}", bot.BotId, ex.Message);
            }
            finally
            {
                lock (_botsLock)
                {
                    _startedBotIds.Remove(bot.BotId);
                    if (bot.Status == BotStatus.Completed || bot.Status == BotStatus.Error)
                    {
                        IBot botToRemove = Bots.FirstOrDefault(x => x.BotId == bot.BotId);
                        if (botToRemove != null)
                            Bots.Remove(botToRemove);
                    }
                }
            }
        }

        private void RemoveFinishedBotsUnsafe()
        {
            List<IBot> botsCompleted = Bots.Where(x => x.Status == BotStatus.Completed || x.Status == BotStatus.Error).ToList();
            foreach (IBot bot in botsCompleted)
            {
                Bots.Remove(bot);
                _startedBotIds.Remove(bot.BotId);
            }
        }

        private async Task OnDeliveryFeedUpdate(BackupRecordDeliveryFeed feed, CancellationToken token)
        {
            try
            {
                if (feed.DeliveryFeedType == DeliveryFeedType.BackupNotify && feed.BackupRecordId > 0)
                {
                    await _backupRecordRepository.UpdateStatusFeedAsync(feed.BackupRecordId, feed.Status.ToString(), feed.Message, feed.ElapsedMilliseconds, feed.NewFilePath);
                }
                else if (feed.DeliveryFeedType == DeliveryFeedType.BackupDeliveryNotify && !string.IsNullOrWhiteSpace(feed.BackupRecordDeliveryId))
                {
                    await _deliveryRecordRepository.UpdateStatusFeedAsync(feed.BackupRecordDeliveryId, feed.Status.ToString(), feed.Message, feed.ElapsedMilliseconds);
                }
                else
                    throw new Exception($"unsupported delivery-feed-type: {feed.DeliveryFeedType}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Updating Status Feed: {Message}", ex.Message);
            }
        }
    }
}
