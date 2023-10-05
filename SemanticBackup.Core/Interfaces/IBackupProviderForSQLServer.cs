using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IBackupProviderForSQLServer
    {
        Task<bool> BackupDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord);
        Task<List<string>> GetAvailableDatabaseCollectionAsync(ResourceGroup resourceGroup);
        Task<bool> RestoreDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord);
        Task<(bool success, string err)> TryTestDbConnectivityAsync(ResourceGroup resourceGroup);
    }
}
