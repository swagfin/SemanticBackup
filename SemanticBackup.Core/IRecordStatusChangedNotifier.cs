using SemanticBackup.Core.Models;

namespace SemanticBackup.Core
{
    public interface IRecordStatusChangedNotifier
    {
        void DispatchBackupRecordUpdatedStatus(BackupRecord backupRecord, bool isNewRecord = false);
        void DispatchContentDeliveryUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord = false);
    }
}
