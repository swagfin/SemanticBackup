using SemanticBackup.Core.Models;

namespace SemanticBackup.Core
{
    public interface IRecordStatusChangedNotifier
    {
        void DispatchUpdatedStatus(BackupRecord backupRecord, bool isNewRecord = false);
        void DispatchUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord = false);
    }
}
