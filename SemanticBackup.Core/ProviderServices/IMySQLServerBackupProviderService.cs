using SemanticBackup.Core.Models;

namespace SemanticBackup.Core.ProviderServices
{
    public interface IMySQLServerBackupProviderService
    {
        bool BackupDatabase(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
    }
}
