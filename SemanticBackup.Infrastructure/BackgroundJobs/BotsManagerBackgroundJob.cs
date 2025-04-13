using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Infrastructure.BackgroundJobs.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs
{
    public class BotsManagerBackgroundJob : IHostedService
    {
        private readonly ILogger<BotsManagerBackgroundJob> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private List<IBot> Bots { get; set; } = [];

        public BotsManagerBackgroundJob(ILogger<BotsManagerBackgroundJob> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Bots = [];
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
            if (botIds != null && botIds.Count > 0)
            {
                List<IBot> botsToRemove = this.Bots.Where(x => botIds.Contains(x.BotId)).ToList();
                foreach (IBot bot in botsToRemove)
                    this.Bots.Remove(bot);
            }
        }

        public bool HasAvailableResourceGroupBotsCount(string resourceGroupId, int maximumThreads = 1)
        {
            int resourceBots = this.Bots.Where(x => x.ResourceGroupId == resourceGroupId).Count();
            int runningResourceGrpThreads = resourceBots;
            int availableResourceGrpThreads = maximumThreads - runningResourceGrpThreads;
            return availableResourceGrpThreads > 0;
        }
        private void SetupBotsBackgroundService(CancellationToken cancellationToken)
        {
            var t = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (this.Bots != null && this.Bots.Count > 0)
                        {
                            //Start and Stop Bacup Bots
                            List<IBot> botsNotReady = (Bots.Where(x => x.Status == BotStatus.NotReady).OrderBy(x => x.DateCreatedUtc).ToList()) ?? [];
                            foreach (IBot bot in botsNotReady)
                            {
                                _ = bot.RunAsync(OnDeliveryFeedUpdate, cancellationToken);
                            }
                            //Remove Completed
                            List<IBot> botsCompleted = this.Bots.Where(x => x.IsCompleted).ToList();
                            if (botsCompleted != null && botsCompleted.Count > 0)
                                foreach (IBot bot in botsCompleted)
                                    this.Bots.Remove(bot);
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning($"Running Unstarted and Removing Completed Bots Failed: {ex.Message}"); }
                    //Delay
                    await Task.Delay(5000);
                }
            });
            t.Start();
        }

        private async Task OnDeliveryFeedUpdate(BackupRecordDeliveryFeed feed, CancellationToken token)
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IContentDeliveryRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                await _persistanceService.UpdateStatusFeedAsync(feed.BackupRecordDeliveryId, feed.Status.ToString(), feed.Message, feed.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Updating Status Feed: {Message}", ex.Message);
            }
        }
    }
}
