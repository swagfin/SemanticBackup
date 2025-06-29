using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
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
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _deliveryRecordRepository;
        private List<IBot> Bots { get; set; } = [];

        public BotsManagerBackgroundJob(ILogger<BotsManagerBackgroundJob> logger, IResourceGroupRepository resourceGroupRepository, IBackupRecordRepository backupRecordRepository, IContentDeliveryRecordRepository deliveryRecordRepository)
        {
            _logger = logger;
            _resourceGroupRepository = resourceGroupRepository;
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

        public void AddBot(IBot bot) => Bots.Add(bot);
        public void AddBot(List<IBot> bots) => Bots.AddRange(bots);
        public void TerminateBots(List<string> botIds)
        {
            if (botIds.Count != 0)
            {
                List<IBot> botsToRemove = [.. Bots.Where(x => botIds.Contains(x.BotId))];
                foreach (IBot bot in botsToRemove)
                    Bots.Remove(bot);
            }
        }

        private void SetupBotsBackgroundService(CancellationToken cancellationToken)
        {
            Thread t = new(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //Delay
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    try
                    {
                        //Remove Bots with status: [Completed] or [Error]
                        List<IBot> botsCompleted = (Bots.Where(x => x.Status == BotStatus.Completed || x.Status == BotStatus.Error).ToList()) ?? [];
                        foreach (IBot bot in botsCompleted)
                            this.Bots.Remove(bot);

                        //Start Bots with status: [PendingStart]
                        List<IBot> botsNotReady = (Bots.Where(x => x.Status == BotStatus.PendingStart).OrderBy(x => x.DateCreatedUtc).ToList()) ?? [];
                        foreach (IBot bot in botsNotReady)
                        {
                            ResourceGroup resourceGroup = await _resourceGroupRepository.GetByIdOrKeyAsync(bot.ResourceGroupId ?? string.Empty);
                            int runningPods = Bots.Count(x => x.ResourceGroupId == resourceGroup.Id && x.Status != BotStatus.PendingStart);
                            if (runningPods < resourceGroup.MaximumRunningBots)
                            {
                                Debug.WriteLine($"Running bot #{bot.BotId}");
                                _ = bot.RunAsync(OnDeliveryFeedUpdate, cancellationToken);
                            }
                            else
                                Debug.WriteLine($"[{nameof(BotsManagerBackgroundJob)}] Resource Group({resourceGroup.Id}) {runningPods}-Bot(s) are Busy, maximum pods configured to: {resourceGroup.MaximumRunningBots}, waiting for available Bots....");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error: {Message}", ex.Message);
                    }
                }
            });
            t.Start();
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
