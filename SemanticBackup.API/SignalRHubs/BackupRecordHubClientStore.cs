using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.API.SignalRHubs
{
    public class BackupRecordHubClientStore
    {

        public static class BackupRecordHubClientStorage
        {
            private static readonly object cLock = new object();
            private static List<ClientGroup> ClientGroups = new List<ClientGroup>();

            public static List<ClientGroup> GetClientGroups() { return ClientGroups; }

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
                        clientGroup = new ClientGroup()
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
}
