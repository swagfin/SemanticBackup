using System;
using System.Collections.Generic;

namespace SemanticBackup.API.SignalRHubs
{
    public class ClientGroup
    {
        public string Name { get; set; }
        public DateTime? LastRefreshUTC { get; set; } = null;
        public HashSet<string> Clients { get; set; } = new HashSet<string>();
    }
}
