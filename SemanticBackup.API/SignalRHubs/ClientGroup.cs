using System;
using System.Collections.Generic;

namespace SemanticBackup.API.SignalRHubs
{
    public class ClientGroup
    {
        public string Name { get; set; }
        public DateTime? LastRefresh { get; set; }
        public HashSet<string> Clients { get; set; } = new HashSet<string>();
    }
}
