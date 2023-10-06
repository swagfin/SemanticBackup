using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupRecordDelivery
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        [Required]
        public long BackupRecordId { get; set; }
        [Required]
        public string DeliveryType { get; set; }
        public string CurrentStatus { get; set; } = BackupRecordDeliveryStatus.QUEUED.ToString();
        public DateTime StatusUpdateDateUTC { get; set; } = DateTime.UtcNow;
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public DateTime RegisteredDateUTC { get; set; } = DateTime.UtcNow;
    }
    public enum BackupRecordDeliveryStatus
    {
        QUEUED,
        EXECUTING,
        READY,
        ERROR
    }
}
