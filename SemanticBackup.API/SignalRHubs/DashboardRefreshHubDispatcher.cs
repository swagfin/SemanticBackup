using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.API.Extensions;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SemanticBackup.API.SignalRHubs.DashboardRefreshHubClientStore;

namespace SemanticBackup.API.SignalRHubs
{
    public class DashboardRefreshHubDispatcher : Hub, IProcessorInitializable
    {
        private readonly ILogger<DashboardRefreshHubDispatcher> _logger;
        private readonly IHubContext<DashboardRefreshHubDispatcher> hub;
        private readonly SharedTimeZone _sharedTimeZone;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;

        public DashboardRefreshHubDispatcher(ILogger<DashboardRefreshHubDispatcher> logger,
            IHubContext<DashboardRefreshHubDispatcher> hub,
            SharedTimeZone sharedTimeZone,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IBackupRecordPersistanceService backupRecordPersistanceService
            )
        {
            this._logger = logger;
            this.hub = hub;
            this._sharedTimeZone = sharedTimeZone;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
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
            DashboardRefreshHubClientStorage.RemoveClient(Context.ConnectionId);
            base.OnDisconnectedAsync(exception);
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"Adding client: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public async Task JoinGroup(JoinRequest joinRequest)
        {
            try
            {
                string group = $"{joinRequest.Directory}#{joinRequest.Group}";
                _logger.LogInformation("Adding user from Group: {group}", joinRequest.Directory, joinRequest.Group);
                DashboardRefreshHubClientStorage.AddClient(group, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
                //Push Data to New
                _logger.LogInformation($"Pushing Data to newly Joined Client under group {group}");
                SendMetrics(group);
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
                        //Dispatch Data to Users
                        var groups = DashboardRefreshHubClientStorage.GetClientGroups().Where(x => x.Clients.Any());
                        foreach (var group in groups)
                            SendMetrics(group.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    //Sleep
                    Thread.Sleep(30000);
                }
            });
            t.Start();
        }

        private void SendMetrics(string groupRecord)
        {
            try
            {
                string[] groupRecordParams = groupRecord.Split('#');
                if (groupRecordParams.Length < 2)
                {
                    _logger.LogWarning("Terminated Dispatch, Reason Group Record was not in the correct format");
                    return;
                }
                string directory = groupRecordParams[0];
                string subscriberGroup = groupRecordParams[1];

                _logger.LogInformation("Preparing to send Metrics for Group: {group}", groupRecord);

                DashboardClientGroup clientGrp = DashboardRefreshHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == groupRecord);
                //DateTime
                DateTime currentTime = _sharedTimeZone.Now;
                DateTime metricsFromDate = currentTime.AddHours(-24);// 24hrs Ago

                RealTimeViewModel lastAVGMetric = clientGrp.Metric.AvgMetrics.OrderBy(x => x.TimeStamp).LastOrDefault();
                if (lastAVGMetric != null)
                    metricsFromDate = lastAVGMetric.TimeStamp;

                var recordsLatest = _backupRecordPersistanceService.GetAllByRegisteredDateByStatus(directory, metricsFromDate, subscriberGroup);
                if (recordsLatest != null && recordsLatest.Count > 0)
                    foreach (var record in recordsLatest)
                    {
                        var existingMetric = clientGrp.Metric.AvgMetrics.FirstOrDefault(x => x.TimeStampCurrent == record.StatusUpdateDate.IgnoreSeconds(false).ToString("hh tt"));
                        if (existingMetric == null)
                            clientGrp.Metric.AvgMetrics.Add(new RealTimeViewModel
                            {
                                TimeStamp = record.RegisteredDate,
                                SuccessCount = 1,
                                ErrorsCount = 0,
                                TimeStampCurrent = record.RegisteredDate.IgnoreSeconds(false).ToString("hh tt")
                            });
                        else
                        {
                            existingMetric.SuccessCount += 1;
                            existingMetric.TimeStamp = (existingMetric.TimeStamp < record.RegisteredDate) ? record.RegisteredDate : existingMetric.TimeStamp;
                        }
                    }

                var recordsFailsLatest = _backupRecordPersistanceService.GetAllByStatusUpdateDateByStatus(directory, metricsFromDate, BackupRecordBackupStatus.ERROR.ToString());
                if (recordsFailsLatest != null && recordsFailsLatest.Count > 0)
                    foreach (var record in recordsFailsLatest)
                    {
                        var existingMetric = clientGrp.Metric.AvgMetrics.FirstOrDefault(x => x.TimeStampCurrent == record.StatusUpdateDate.IgnoreSeconds(false).ToString("hh tt"));
                        if (existingMetric == null)
                            clientGrp.Metric.AvgMetrics.Add(new RealTimeViewModel
                            {
                                TimeStamp = record.StatusUpdateDate,
                                SuccessCount = 0,
                                ErrorsCount = 1,
                                TimeStampCurrent = record.StatusUpdateDate.IgnoreSeconds(false).ToString("hh tt")
                            });
                        else
                        {
                            existingMetric.ErrorsCount += 1;
                            existingMetric.TimeStamp = (existingMetric.TimeStamp < record.StatusUpdateDate) ? record.StatusUpdateDate : existingMetric.TimeStamp;
                        }
                    }

                //Set Last Update Time
                clientGrp.LastRefresh = currentTime;
                //Lets Remove Metrics Surpassed  more than 24hrs to avoid Memory Overload
                clientGrp.Metric.AvgMetrics.RemoveAll(x => x.TimeStamp < currentTime.AddHours(-24));
                clientGrp.Metric.TotalBackupSchedules = _backupSchedulePersistanceService.GetAll(directory).Count();
                clientGrp.Metric.TotalDatabases = _databaseInfoPersistanceService.GetAll(directory).Count();
                clientGrp.Metric.TotalBackupRecords = _backupRecordPersistanceService.GetAll(directory).Count();

                _ = hub.Clients.Group(groupRecord).SendAsync("ReceiveMetrics", clientGrp.Metric);

                _logger.LogInformation("Successfully sent Metrics for Group: {group}", groupRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                //throw;
            }
        }

    }
}