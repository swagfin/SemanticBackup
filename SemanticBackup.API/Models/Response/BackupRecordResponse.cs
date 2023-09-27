using System;

namespace SemanticBackup.Core.Models.Response
{
    public class BackupRecordResponse
    {
        public string Id { get; set; }
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string BackupStatus { get; set; }
        public string Path { get; set; }
        public DateTime StatusUpdateDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public bool ExecutedDeliveryRun { get; set; } = false;
        public DateTime RegisteredDate { get; set; }
    }
}
