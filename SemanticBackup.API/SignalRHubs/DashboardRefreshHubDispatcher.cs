using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.API.Extensions;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
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
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;

        public DashboardRefreshHubDispatcher(ILogger<DashboardRefreshHubDispatcher> logger,
            IHubContext<DashboardRefreshHubDispatcher> hub,
            SharedTimeZone sharedTimeZone,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IBackupRecordPersistanceService backupRecordPersistanceService, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this._logger = logger;
            this.hub = hub;
            this._sharedTimeZone = sharedTimeZone;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
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
                string group = $"{joinRequest.Resourcegroup}#{joinRequest.Group}";
                _logger.LogInformation("Adding user from Group: {group}", joinRequest.Resourcegroup, joinRequest.Group);
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
                string resourcegroup = groupRecordParams[0];
                string subscriberGroup = groupRecordParams[1];
                //Resource Group Name
                _logger.LogInformation("Preparing to send Metrics for Group: {group}", groupRecord);
                //Resource Group Timezone
                //Change UTC now to Resource Group TimeZone
                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(resourcegroup);
                DateTime currentTimeLocal = _sharedTimeZone.GetLocalTimeByTimezone(resourceGroup?.TimeZone);

                DashboardClientGroup clientGrp = DashboardRefreshHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == groupRecord);
                DateTime metricsFromDateLocal = currentTimeLocal.AddHours(-24);// 24hrs Ago
                //Clear All
                clientGrp.Metric.AvgMetrics = new List<RealTimeViewModel>();

                var recordsLatest = _backupRecordPersistanceService.GetAllByRegisteredDateByStatus(resourcegroup, metricsFromDateLocal, subscriberGroup);
                if (recordsLatest != null && recordsLatest.Count > 0)
                    foreach (var record in recordsLatest)
                    {
                        var existingMetric = clientGrp.Metric.AvgMetrics.FirstOrDefault(x => x.TimeStampCurrent == record.StatusUpdateDateUTC.IgnoreSeconds(false).ToString("hh tt"));
                        if (existingMetric == null)
                            clientGrp.Metric.AvgMetrics.Add(new RealTimeViewModel
                            {
                                TimeStamp = record.RegisteredDateUTC,
                                SuccessCount = 1,
                                ErrorsCount = 0,
                                TimeStampCurrent = record.RegisteredDateUTC.IgnoreSeconds(false).ToString("hh tt")
                            });
                        else
                        {
                            existingMetric.SuccessCount += 1;
                            existingMetric.TimeStamp = (existingMetric.TimeStamp < record.RegisteredDateUTC) ? record.RegisteredDateUTC : existingMetric.TimeStamp;
                        }
                    }

                var recordsFailsLatest = _backupRecordPersistanceService.GetAllByStatusUpdateDateByStatus(resourcegroup, metricsFromDateLocal, BackupRecordBackupStatus.ERROR.ToString());
                if (recordsFailsLatest != null && recordsFailsLatest.Count > 0)
                    foreach (var record in recordsFailsLatest)
                    {
                        var existingMetric = clientGrp.Metric.AvgMetrics.FirstOrDefault(x => x.TimeStampCurrent == record.StatusUpdateDateUTC.IgnoreSeconds(false).ToString("hh tt"));
                        if (existingMetric == null)
                            clientGrp.Metric.AvgMetrics.Add(new RealTimeViewModel
                            {
                                TimeStamp = record.StatusUpdateDateUTC,
                                SuccessCount = 0,
                                ErrorsCount = 1,
                                TimeStampCurrent = record.StatusUpdateDateUTC.IgnoreSeconds(false).ToString("hh tt")
                            });
                        else
                        {
                            existingMetric.ErrorsCount += 1;
                            existingMetric.TimeStamp = (existingMetric.TimeStamp < record.StatusUpdateDateUTC) ? record.StatusUpdateDateUTC : existingMetric.TimeStamp;
                        }
                    }

                //Set Last Update Time
                clientGrp.LastRefreshUTC = DateTime.UtcNow;
                //Lets Remove Metrics Surpassed  more than 24hrs to avoid Memory Overload
                clientGrp.Metric.AvgMetrics.RemoveAll(x => x.TimeStamp < currentTimeLocal.AddHours(-24));
                //Convert Dates from Utc to Local Time
                clientGrp.Metric.AvgMetrics = clientGrp.Metric.AvgMetrics.Select(x => new RealTimeViewModel
                {
                    ErrorsCount = x.ErrorsCount,
                    SuccessCount = x.SuccessCount,
                    TimeStamp = _sharedTimeZone.ConvertUtcDateToLocalTime(x.TimeStamp, resourceGroup?.TimeZone),
                    TimeStampCurrent = _sharedTimeZone.ConvertUtcDateToLocalTime(x.TimeStamp, resourceGroup?.TimeZone).IgnoreSeconds(false).ToString("hh tt")
                }).OrderBy(x => x.TimeStamp).ToList();

                clientGrp.Metric.TotalBackupSchedules = _backupSchedulePersistanceService.GetAll(resourcegroup).Count();
                clientGrp.Metric.TotalDatabases = _databaseInfoPersistanceService.GetAll(resourcegroup).Count();
                clientGrp.Metric.TotalBackupRecords = _backupRecordPersistanceService.GetAll(resourcegroup).Count();

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