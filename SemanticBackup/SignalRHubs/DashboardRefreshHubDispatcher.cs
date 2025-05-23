﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SemanticBackup.SignalRHubs.DashboardRefreshHubClientStore;

namespace SemanticBackup.SignalRHubs
{
    [Authorize]
    public class DashboardRefreshHubDispatcher : Hub, IHostedService
    {
        private readonly ILogger<DashboardRefreshHubDispatcher> _logger;
        private readonly IHubContext<DashboardRefreshHubDispatcher> hub;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DashboardRefreshHubDispatcher(ILogger<DashboardRefreshHubDispatcher> logger, IHubContext<DashboardRefreshHubDispatcher> hub, IServiceScopeFactory serviceScopeFactory)
        {
            this._logger = logger;
            this.hub = hub;
            this._serviceScopeFactory = serviceScopeFactory;
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
            DashboardRefreshHubClientStorage.RemoveClient(Context.ConnectionId);
            base.OnDisconnectedAsync(exception);
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public async Task JoinGroup(JoinRequest joinRequest)
        {
            try
            {
                string group = $"{joinRequest.Resourcegroup}#{joinRequest.Group}";
                DashboardRefreshHubClientStorage.AddClient(group, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
                _ = SendMetricsAsync(group);
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
                    try
                    {
                        //Dispatch Data to Users
                        IEnumerable<DashboardClientGroup> groups = DashboardRefreshHubClientStorage.GetClientGroups().Where(x => x.Clients.Any());
                        foreach (var group in groups)
                            _ = SendMetricsAsync(group.Name);
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

        private async Task SendMetricsAsync(string groupRecord)
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
                //Resource Group Timezone
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                //DI INJECTIONS
                IResourceGroupRepository resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                IBackupRecordRepository backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                IBackupScheduleRepository backupSchedulePersistanceService = scope.ServiceProvider.GetRequiredService<IBackupScheduleRepository>();
                IDatabaseInfoRepository databaseInfoPersistanceService = scope.ServiceProvider.GetRequiredService<IDatabaseInfoRepository>();

                //Proceed
                ResourceGroup resourceGroup = await resourceGroupPersistanceService.GetByIdOrKeyAsync(resourcegroup);
                DateTime currentTimeUTC = DateTime.UtcNow;

                DashboardClientGroup clientGrp = DashboardRefreshHubClientStorage.GetClientGroups().FirstOrDefault(x => x.Name == groupRecord);
                DateTime metricsFromDatUTC = currentTimeUTC.AddHours(-24);
                clientGrp.Metric.AvgMetrics = new List<RealTimeViewModel>();

                var recordsLatest = await backupRecordPersistanceService.GetAllByRegisteredDateByStatusAsync(resourcegroup, metricsFromDatUTC, subscriberGroup);
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

                var recordsFailsLatest = await backupRecordPersistanceService.GetAllByStatusUpdateDateByStatusAsync(resourcegroup, metricsFromDatUTC, BackupRecordStatus.ERROR.ToString());
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
                clientGrp.LastRefreshUTC = currentTimeUTC;
                //Convert Dates from Utc to Local Time
                clientGrp.Metric.AvgMetrics = clientGrp.Metric.AvgMetrics.Select(x => new RealTimeViewModel
                {
                    ErrorsCount = x.ErrorsCount,
                    SuccessCount = x.SuccessCount,
                    TimeStamp = x.TimeStamp,
                    TimeStampCurrent = x.TimeStamp.IgnoreSeconds(false).ToString("hh tt")
                }).OrderBy(x => x.TimeStamp).ToList();

                clientGrp.Metric.TotalBackupSchedules = await backupSchedulePersistanceService.GetAllCountAsync(resourcegroup);
                clientGrp.Metric.TotalDatabases = await databaseInfoPersistanceService.GetAllCountAsync(resourcegroup);
                clientGrp.Metric.TotalBackupRecords = await backupRecordPersistanceService.GetAllCountAsync(resourcegroup);

                _ = hub.Clients.Group(groupRecord).SendAsync("ReceiveMetrics", clientGrp.Metric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}