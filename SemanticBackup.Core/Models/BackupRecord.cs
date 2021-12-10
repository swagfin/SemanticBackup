using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupRecord
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string BackupStatus { get; set; } = BackupRecordBackupStatus.QUEUED.ToString();
        [Required]
        public string Path { get; set; }
        public DateTime StatusUpdateDateUTC { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDateUTC { get; set; } = DateTime.UtcNow.AddDays(7);
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public DateTime RegisteredDateUTC { get; set; } = DateTime.UtcNow;
    }
    public enum BackupRecordBackupStatus
    {
        QUEUED,
        EXECUTING,
        COMPLETED,
        COMPRESSING,
        READY,
        ERROR
    }
}
