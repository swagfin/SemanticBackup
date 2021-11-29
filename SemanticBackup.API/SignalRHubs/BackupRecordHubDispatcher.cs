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
        private ConcurrentQueue<BackupRecord> BackupRecordsQueue = new ConcurrentQueue<BackupRecord>();

        public BackupRecordHubDispatcher(ILogger<BackupRecordHubDispatcher> logger, IHubContext<BackupRecordHubDispatcher> hub, SharedTimeZone sharedTimeZone)
        {
            this._logger = logger;
            this.hub = hub;
            this._sharedTimeZone = sharedTimeZone;
            BackupRecordsQueue = new ConcurrentQueue<BackupRecord>();
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
        public void DispatchUpdatedStatus(BackupRecord backupRecord)
        {
            try
            {
                BackupRecordsQueue.Enqueue(backupRecord);
            }
            catch { }
        }
        public void DispatchDeletedStatus(string recordId)
        {
            return; //No Notification
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
                            if (BackupRecordsQueue.TryDequeue(out BackupRecord backupRecord) && backupRecord != null)
                            {
                                ClientGroup clientGrp = BackupRecordHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == backupRecord.Id);
                                if (clientGrp != null)
                                    SendNotification(clientGrp, backupRecord);
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

        private void SendNotification(ClientGroup clientGrp, BackupRecord backupRecord)
        {
            try
            {

                _logger.LogInformation("Sending Metrics to Connected: {group}", clientGrp.Name);
                DateTime currentTime = _sharedTimeZone.Now;
                //Set Last Update Time
                clientGrp.LastRefresh = currentTime;
                _ = hub.Clients.Group(clientGrp.Name).SendAsync("ReceiveNotification", backupRecord);
                _logger.LogInformation("Successfully sent Metrics for Group: {group}", clientGrp.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                //throw;
            }
        }

    }
}
