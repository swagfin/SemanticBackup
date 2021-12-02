using SemanticBackup.Core.Models;

namespace SemanticBackup.Core
{
    public interface IBackupRecordStatusChangedNotifier
    {
        void DispatchUpdatedStatus(BackupRecord backupRecord, bool isNewRecord = false);
    }
}
