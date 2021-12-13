﻿namespace SemanticBackup.WebClient.Models.Response
{
    public class ResourceGroupResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long LastAccess { get; set; } = 0;
    }
}