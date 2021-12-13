﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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
    public class BackupRecordHubDispatcher : Hub, IRecordStatusChangedNotifier, IProcessorInitializable
    {
        private readonly ILogger<BackupRecordHubDispatcher> _logger;
        private readonly IHubContext<BackupRecordHubDispatcher> hub;
        private ConcurrentQueue<BackupRecordMetric> BackupRecordsQueue = new ConcurrentQueue<BackupRecordMetric>();

        public BackupRecordHubDispatcher(ILogger<BackupRecordHubDispatcher> logger, IHubContext<BackupRecordHubDispatcher> hub)
        {
            this._logger = logger;
            this.hub = hub;
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
                                ClientGroup allClientGroups = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupMetricRecord.Metric.ResourceGroupId);
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
                //Set Last Update Time
                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                backupRecordMetric.LastSyncDateUTC = DateTime.UtcNow;
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

        public void DispatchUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord)
        {
            throw new NotImplementedException();
        }
    }
    public class BackupRecordMetric
    {
        public string Subscription { get; set; }
        public BackupRecord Metric { get; set; } = null;
        public DateTime LastSyncDateUTC { get; set; } = DateTime.UtcNow;
        public bool IsNewMetric { get; set; } = false;
    }
}
