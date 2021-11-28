using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupRecord
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } = BackupRecordType.QUEUED.ToString();
        [Required]
        public string Path { get; set; }
        public DateTime StatusUpdateDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; } = null;
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
    }
    public enum BackupRecordType
    {
        QUEUED,
        EXECUTING,
        COMPLETED,
        ERROR
    }
}
