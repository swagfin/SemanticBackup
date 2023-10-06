using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IContentDeliveryRecordRepository
    {
        Task<List<BackupRecordDelivery>> GetAllAsync(string resourceGroupId);
        Task<BackupRecordDelivery> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(BackupRecordDelivery record);
        Task<bool> UpdateAsync(BackupRecordDelivery record);
        Task<bool> UpdateStatusFeedAsync(string id, string status, string message = null, long executionInMilliseconds = 0);
        Task<List<BackupRecordDelivery>> GetAllByStatusAsync(string status);
        Task<List<BackupRecordDelivery>> GetAllByBackupRecordIdAsync(long id);
        Task<List<BackupRecordDelivery>> GetAllByBackupRecordIdByStatusAsync(string resourceGroupId, long id, string status = "*");
        Task<BackupRecordDelivery> GetByContentTypeByExecutionMessageAsync(string deliveryType, string executionMessage);
        Task<List<string>> GetAllNoneResponsiveAsync(List<string> statusChecks, int minuteDifference);
    }
}
