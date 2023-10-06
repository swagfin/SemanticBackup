using SemanticBackup.Core.Models;

namespace SemanticBackup.Core
{
    public interface IRecordStatusChangedNotifier
    {
        void DispatchBackupRecordUpdatedStatus(BackupRecord backupRecord, bool isNewRecord = false);
        void DispatchContentDeliveryUpdatedStatus(BackupRecordDelivery record, bool isNewRecord = false);
    }
}
