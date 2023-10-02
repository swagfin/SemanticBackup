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
        public string BackupStatus { get; set; } = BackupRecordBackupStatus.QUEUED.ToString();
        [Required]
        public string Path { get; set; }
        public DateTime StatusUpdateDateUTC { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDateUTC { get; set; } = DateTime.UtcNow.AddDays(7);
        public string ExecutionMessage { get; set; }
        public string ExecutionMilliseconds { get; set; }
        public bool ExecutedDeliveryRun { get; set; } = false;
        public string ExecutedDeliveryRunStatus { get; set; } = BackupRecordExecutedDeliveryRunStatus.PENDING_EXECUTION.ToString();
        public DateTime RegisteredDateUTC { get; set; } = DateTime.UtcNow;
        public string RestoreStatus { get; set; } = BackupRecordRestoreStatus.NONE.ToString();
        public string RestoreExecutionMessage { get; set; } = string.Empty;
        public string RestoreConfirmationToken { get; set; } = string.Empty;
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
    public enum BackupRecordExecutedDeliveryRunStatus
    {
        PENDING_EXECUTION,
        SKIPPED_EXECUTION,
        SUCCESSFULLY_EXECUTED
    }
    public enum BackupRecordRestoreStatus
    {
        NONE,
        PENDING_CONFIRMATION,
        PENDING_RESTORE,
        EXECUTING_RESTORE,
        RESTORE_COMPLETED,
        FAILED_RESTORE,

    }
}
