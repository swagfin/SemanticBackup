using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IDatabaseInfoPersistanceService
    {
        Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId);
        Task<BackupDatabaseInfo> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record);
        Task<bool> UpdateAsync(BackupDatabaseInfo record);
        Task<int> GetAllCountAsync(string resourceGroupId);
    }
}
