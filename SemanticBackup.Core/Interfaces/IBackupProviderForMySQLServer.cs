using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IBackupProviderForMySQLServer
    {
        Task<bool> BackupDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord);
        Task<List<string>> GetAvailableDatabaseCollectionAsync(ResourceGroup resourceGroup);
        Task<(bool success, string err)> TryTestDbConnectivityAsync(ResourceGroup resourceGroup);
    }
}
