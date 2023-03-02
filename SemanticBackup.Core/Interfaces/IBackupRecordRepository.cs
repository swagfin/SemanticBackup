using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IBackupRecordRepository
    {
        Task<List<BackupRecord>> GetAllAsync(string resourceGroupId);
        Task<BackupRecord> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(BackupRecord record);
        Task<bool> UpdateAsync(BackupRecord record);
        Task<bool> UpdateStatusFeedAsync(string id, string status, string message = null, long executionInMilliseconds = 0, string newFilePath = null);
        Task<bool> UpdateRestoreStatusFeedAsync(string id, string status, string message = null, string confirmationToken = null);
        Task<List<BackupRecord>> GetAllByStatusAsync(string status);
        Task<List<BackupRecord>> GetAllByDatabaseIdAsync(string id);
        Task<List<BackupRecord>> GetAllByRegisteredDateByStatusAsync(string resourceGroupId, DateTime fromDate, string status = "*");
        Task<List<BackupRecord>> GetAllByStatusUpdateDateByStatusAsync(string resourceGroupId, DateTime fromDate, string status = "*");
        Task<List<BackupRecord>> GetAllExpiredAsync();
        Task<List<BackupRecord>> GetAllByDatabaseIdByStatusAsync(string resourceGroupId, string id, string status = "*");
        Task<List<BackupRecord>> GetAllReadyAndPendingDeliveryAsync();
        Task<bool> UpdateDeliveryRunnedAsync(string backupRecordId, bool hasRun, string executedDeliveryRunStatus);
        Task<List<string>> GetAllNoneResponsiveIdsAsync(List<string> statusChecks, int minuteDifference);
        Task<int> GetAllCountAsync(string resourcegroup);
        Task<List<BackupRecord>> GetAllByRestoreStatusAsync(string status);
    }
}
