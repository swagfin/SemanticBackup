﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SemanticBackup.SignalRHubs.RecordHubClientStore;

namespace SemanticBackup.SignalRHubs
{
    public class RecordStatusChangedHubDispatcher : Hub, IRecordStatusChangedNotifier, IHostedService
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Setting up content dispatcher...");
            StartDispatchDataToConnectedUsers(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            BackupRecordHubClientStorage.RemoveClient(Context.ConnectionId);
            base.OnDisconnectedAsync(exception);
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
        public void DispatchBackupRecordUpdatedStatus(BackupRecord backupRecord, bool isNewRecord)
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

        public void DispatchContentDeliveryUpdatedStatus(BackupRecordDelivery record, bool isNewRecord)
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
                BackupRecordHubClientStorage.AddClient(group, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        private void StartDispatchDataToConnectedUsers(CancellationToken cancellationToken)
        {
            var t = new Thread(() =>
                {
                    while (!cancellationToken.IsCancellationRequested)
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

                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                contentDeliveryRecord.LastSyncDateUTC = DateTime.UtcNow;
                contentDeliveryRecord.Subscription = clientGrp.Name;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveContentDeliveryNotification", contentDeliveryRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private void SendNotification(ClientGroup clientGrp, BackupRecordMetric backupRecordMetric)
        {
            try
            {
                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                backupRecordMetric.LastSyncDateUTC = DateTime.UtcNow;
                backupRecordMetric.Subscription = clientGrp.Name;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveNotification", backupRecordMetric); ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
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
        public BackupRecordDelivery Metric { get; set; } = null;
        public DateTime LastSyncDateUTC { get; set; } = DateTime.UtcNow;
        public bool IsNewMetric { get; set; } = false;
    }
}
