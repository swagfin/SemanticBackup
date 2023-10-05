using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SemanticBackup.SignalRHubs.RecordHubClientStore;

namespace SemanticBackup.SignalRHubs
{
    public class RecordStatusChangedHubDispatcher : Hub, IRecordStatusChangedNotifier, IProcessorInitializable
    {
        private readonly ILogger<RecordStatusChangedHubDispatcher> _logger;
        private readonly IHubContext<RecordStatusChangedHubDispatcher> hub;
        private ConcurrentQueue<BackupRecordMetric> BackupRecordsQueue = new ConcurrentQueue<BackupRecordMetric>();
        private ConcurrentQueue<ContentDeliveryRecordMetric> ContentDeliveryRecordsQueue = new ConcurrentQueue<ContentDeliveryRecordMetric>();

        public RecordStatusChangedHubDispatcher(ILogger<RecordStatusChangedHubDispatcher> logger, IHubContext<RecordStatusChangedHubDispatcher> hub)
        {
            this._logger = logger;
            this.hub = hub;
            BackupRecordsQueue = new ConcurrentQueue<BackupRecordMetric>();
            ContentDeliveryRecordsQueue = new ConcurrentQueue<ContentDeliveryRecordMetric>();
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
        public void DispatchBackupRecordUpdatedStatus(BackupRecord backupRecord, bool isNewRecord)
        {
            try
            {
                BackupRecordsQueue.Enqueue(new BackupRecordMetric
                {
                    Metric = backupRecord,
                    IsNewMetric = isNewRecord,

                });
            }
            catch { }
        }

        public void DispatchContentDeliveryUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord)
        {
            try
            {
                ContentDeliveryRecordsQueue.Enqueue(new ContentDeliveryRecordMetric
                {
                    Metric = record,
                    IsNewMetric = isNewRecord,
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
                        #region Dequeue and Dispatch BackupRecord Status Changed
                        try
                        {
                            if (BackupRecordsQueue.TryDequeue(out BackupRecordMetric backupMetricRecord) && backupMetricRecord != null)
                            {
                                //Specific Group By BackupRecord ID
                                ClientGroup clientGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupMetricRecord.Metric.Id.ToString());
                                if (clientGrp != null)
                                    SendNotification(clientGrp, backupMetricRecord);
                                //Specific Group By Database ID
                                ClientGroup databaseGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupMetricRecord.Metric.BackupDatabaseInfoId);
                                if (databaseGrp != null)
                                    SendNotification(databaseGrp, backupMetricRecord);
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Dequeue and Dispatch Backup Records Error: {ex.Message}");
                        }
                        #endregion

                        #region Dequeue and Dispatch Content Delivery Status Changed
                        try
                        {
                            if (ContentDeliveryRecordsQueue.TryDequeue(out ContentDeliveryRecordMetric contentDeliveryRecord) && contentDeliveryRecord != null)
                            {
                                //Specific Group By BackupRecord ID
                                ClientGroup clientGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == contentDeliveryRecord.Metric.BackupRecordId.ToString());
                                if (clientGrp != null)
                                    SendNotification(clientGrp, contentDeliveryRecord);
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Dequeue and Dispatch Backup Records Error: {ex.Message}");
                        }
                        #endregion

                        //Default Sleep
                        Thread.Sleep(1000);
                    }
                });
            t.Start();
        }

        private void SendNotification(ClientGroup clientGrp, ContentDeliveryRecordMetric contentDeliveryRecord)
        {
            try
            {

                _logger.LogInformation("Sending Content Delivery Notification to Connected: {group}", clientGrp.Name);
                //Set Last Update Time
                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                contentDeliveryRecord.LastSyncDateUTC = DateTime.UtcNow;
                contentDeliveryRecord.Subscription = clientGrp.Name;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveContentDeliveryNotification", contentDeliveryRecord); ;
                _logger.LogInformation("Successfully sent Content Delivery Notification for Group: {group}", clientGrp.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                //throw;
            }
        }

        private void SendNotification(ClientGroup clientGrp, BackupRecordMetric backupRecordMetric)
        {
            try
            {

                _logger.LogInformation("Sending Backup Record Notification to Connected: {group}", clientGrp.Name);
                //Set Last Update Time
                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                backupRecordMetric.LastSyncDateUTC = DateTime.UtcNow;
                backupRecordMetric.Subscription = clientGrp.Name;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveNotification", backupRecordMetric); ;
                _logger.LogInformation("Successfully sent Backup Record Notification for Group: {group}", clientGrp.Name);
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
        public DateTime LastSyncDateUTC { get; set; } = DateTime.UtcNow;
        public bool IsNewMetric { get; set; } = false;
    }
    public class ContentDeliveryRecordMetric
    {
        public string Subscription { get; set; }
        public ContentDeliveryRecord Metric { get; set; } = null;
        public DateTime LastSyncDateUTC { get; set; } = DateTime.UtcNow;
        public bool IsNewMetric { get; set; } = false;
    }
}
