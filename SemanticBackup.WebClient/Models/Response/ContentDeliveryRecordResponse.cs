using System;

namespace SemanticBackup.WebClient.Models.Response
{
    public class ContentDeliveryRecordResponse
    {
        public string Id { get; set; }
        public string ResourceGroupId { get; set; }
        public string BackupRecordId { get; set; }
        public string ContentDeliveryConfigurationId { get; set; }
        public string DeliveryType { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime StatusUpdateDateUTC { get; set; }
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public DateTime RegisteredDateUTC { get; set; }
    }
}
