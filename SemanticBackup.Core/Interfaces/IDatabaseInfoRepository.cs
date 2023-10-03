using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IDatabaseInfoRepository
    {
        Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId);
        Task<BackupDatabaseInfo> GetByIdAsync(string databaseIdentifier);
        Task<BackupDatabaseInfo> GetByDatabaseNameAsync(string databaseName, string databaseType);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record);
        Task<bool> UpdateAsync(BackupDatabaseInfo record);
        Task<int> GetAllCountAsync(string resourceGroupId);
        Task<BackupDatabaseInfo> VerifyDatabaseInResourceGroupThrowIfNotExistAsync(string resourceGroupId, string databaseIdentifier);
        Task<List<string>> GetDatabaseIdsForResourceGroupAsync(string resourceGroupId);
    }
}
