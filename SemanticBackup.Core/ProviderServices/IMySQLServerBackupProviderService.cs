using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.ProviderServices
{
    public interface IMySQLServerBackupProviderService
    {
        bool BackupDatabase(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<IEnumerable<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo);
    }
}
