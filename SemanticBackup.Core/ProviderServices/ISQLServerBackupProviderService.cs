using SemanticBackup.Core.Models;

namespace SemanticBackup.Core.ProviderServices
{
    public interface ISQLServerBackupProviderService
    {
        bool BackupDatabase(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
    }
}
