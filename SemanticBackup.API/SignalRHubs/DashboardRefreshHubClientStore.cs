using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.API.SignalRHubs
{
    public class DashboardRefreshHubClientStore
    {

        public static class DashboardRefreshHubClientStorage
        {
            private static readonly object cLock = new object();
            private static List<DashboardClientGroup> ClientGroups = new List<DashboardClientGroup>();

            public static List<DashboardClientGroup> GetClientGroups() { return ClientGroups; }

            public static void RemoveClient(string connectionId)
            {
                lock (cLock)
                {
                    var group = ClientGroups.Where(x => x.Clients.Any(y => y == connectionId)).FirstOrDefault();
                    if (group != null)
                    {
                        var client = group.Clients.FirstOrDefault(x => x == connectionId);
                        if (client != null)
                            group.Clients.Remove(client);
                    }

                }
            }

            public static void AddClient(string group, string connectionId)
            {
                lock (cLock)
                {
                    var clientGroup = ClientGroups.FirstOrDefault(x => x.Name == group);

                    if (clientGroup == null)
                    {
                        clientGroup = new DashboardClientGroup()
                        {
                            Name = group
                        };
                        clientGroup.Clients.Add(connectionId);

                        ClientGroups.Add(clientGroup);
                    }
                    else
                    {
                        clientGroup.Clients.Add(connectionId);
                    }
                }

            }
        }

    }

    public class DashboardClientGroup : ClientGroup
    {
        public Metric Metric { get; set; } = new Metric();
    }
    public class Metric
    {
        public Metric()
        {
            AvgMetrics = new List<RealTimeViewModel>();
        }
        public double TotalDatabases { get; set; }
        public double TotalBackupRecords { get; set; }
        public double TotalBackupSchedules { get; set; }
        //Previous
        public List<RealTimeViewModel> AvgMetrics { get; set; }
    }

    public class RealTimeViewModel
    {
        public DateTime TimeStamp { get; set; }
        public double SuccessCount { get; set; } = 0;
        public double ErrorsCount { get; set; } = 0;
        public string TimeStampCurrent { get; set; }
    }
}
