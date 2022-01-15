using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.ProviderServices
{
    public interface ISQLServerBackupProviderService
    {
        Task<bool> BackupDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<bool> RestoreDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<IEnumerable<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo);
    }
}
