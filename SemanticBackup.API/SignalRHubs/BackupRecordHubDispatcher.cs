using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SemanticBackup.API.SignalRHubs.BackupRecordHubClientStore;

namespace SemanticBackup.API.SignalRHubs
{
    public class BackupRecordHubDispatcher : Hub, IBackupRecordStatusChangedNotifier, IProcessorInitializable
    {
        private readonly ILogger<BackupRecordHubDispatcher> _logger;
        private readonly IHubContext<BackupRecordHubDispatcher> hub;
        private readonly SharedTimeZone _sharedTimeZone;
        private ConcurrentQueue<BackupRecordMetric> BackupRecordsQueue = new ConcurrentQueue<BackupRecordMetric>();

        public BackupRecordHubDispatcher(ILogger<BackupRecordHubDispatcher> logger, IHubContext<BackupRecordHubDispatcher> hub, SharedTimeZone sharedTimeZone)
        {
            this._logger = logger;
            this.hub = hub;
            this._sharedTimeZone = sharedTimeZone;
            BackupRecordsQueue = new ConcurrentQueue<BackupRecordMetric>();
        }

        public void Initialize()
        {
            _logger.LogInformation("Setting up content dispatcher...");
            StartDispatchDataToConnectedUsers();
            _logger.LogInformation("Setting up content dispatcher...DONE");
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Removing client: {Context.ConnectionId}");
            BackupRecordHubClientStorage.RemoveClient(Context.ConnectionId);
            base.OnDisconnectedAsync(exception);
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"Adding client: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
        public void DispatchUpdatedStatus(BackupRecord backupRecord, bool isNewRecord)
        {
            try
            {
                BackupRecordsQueue.Enqueue(new BackupRecordMetric
                {
                    Metric = backupRecord,
                    IsNewMetric = isNewRecord
                });
            }
            catch { }
        }

        public async Task JoinGroup(object groupObj)
        {
            try
            {
                string group = groupObj.ToString();
                _logger.LogInformation("Adding user to Group: {group}", group);
                BackupRecordHubClientStorage.AddClient(group, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
                //No New Data to Push Wait for Notification
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        private void StartDispatchDataToConnectedUsers()
        {
            var t = new Thread(() =>
                {
                    while (true)
                    {

                        try
                        {
                            if (BackupRecordsQueue.TryDequeue(out BackupRecordMetric backupMetricRecord) && backupMetricRecord != null)
                            {
                                //Specific Group By BackupRecord ID
                                ClientGroup clientGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupMetricRecord.Metric.Id);
                                if (clientGrp != null)
                                    SendNotification(clientGrp, backupMetricRecord);
                                //Specific Group By Database ID
                                ClientGroup databaseGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupMetricRecord.Metric.BackupDatabaseInfoId);
                                if (databaseGrp != null)
                                    SendNotification(databaseGrp, backupMetricRecord);
                                //All Groups Joined
                                ClientGroup allClientGroups = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == "*");
                                if (allClientGroups != null)
                                    SendNotification(allClientGroups, backupMetricRecord);
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.ToString());
                        }
                        //Default Sleep
                        Thread.Sleep(1000);
                    }
                });
            t.Start();
        }

        private void SendNotification(ClientGroup clientGrp, BackupRecordMetric backupRecordMetric)
        {
            try
            {

                _logger.LogInformation("Sending Metrics to Connected: {group}", clientGrp.Name);
                DateTime currentTime = _sharedTimeZone.Now;
                //Set Last Update Time
                clientGrp.LastRefresh = currentTime;
                backupRecordMetric.LastSyncDate = currentTime;
                backupRecordMetric.Subscription = clientGrp.Name;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveNotification", backupRecordMetric); ;
                _logger.LogInformation("Successfully sent Metrics for Group: {group}", clientGrp.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                //throw;
            }
        }

    }
    public class BackupRecordMetric
    {
        public string Subscription { get; set; }
        public BackupRecord Metric { get; set; } = null;
        public DateTime LastSyncDate { get; set; } = DateTime.Now;
        public bool IsNewMetric { get; set; } = false;
    }
}
