using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IBackupProviderForSQLServer
    {
        Task<bool> BackupDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<bool> RestoreDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<List<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo);
        Task<(bool success, string err)> TryTestConnectionAsync(string connectionString);
    }
}
