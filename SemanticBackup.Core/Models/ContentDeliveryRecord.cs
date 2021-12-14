using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class ContentDeliveryRecord
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        [Required]
        public string BackupRecordId { get; set; }
        [Required]
        public string ContentDeliveryConfigurationId { get; set; }
        [Required]
        public string DeliveryType { get; set; }
        public string CurrentStatus { get; set; } = ContentDeliveryRecordStatus.QUEUED.ToString();
        public DateTime StatusUpdateDateUTC { get; set; } = DateTime.UtcNow;
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public DateTime RegisteredDateUTC { get; set; } = DateTime.UtcNow;
    }
    public enum ContentDeliveryRecordStatus
    {
        QUEUED,
        EXECUTING,
        READY,
        ERROR
    }
}
