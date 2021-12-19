using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupSchedulePersistanceService
    {
        Task<List<BackupSchedule>> GetAllAsync(string resourcegroup);
        Task<BackupSchedule> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(BackupSchedule record);
        Task<bool> UpdateAsync(BackupSchedule record);
        Task<List<BackupSchedule>> GetAllDueByDateAsync();
        Task<List<BackupSchedule>> GetAllByDatabaseIdAsync(string id);
        Task<int> GetAllCountAsync(string resourcegroup);
        Task<bool> UpdateLastRunAsync(string id, DateTime lastRunUTC);
    }
}
