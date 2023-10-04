using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IContentDeliveryRecordRepository
    {
        Task<List<ContentDeliveryRecord>> GetAllAsync(string resourceGroupId);
        Task<ContentDeliveryRecord> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(ContentDeliveryRecord record);
        Task<bool> UpdateAsync(ContentDeliveryRecord record);
        Task<bool> UpdateStatusFeedAsync(string id, string status, string message = null, long executionInMilliseconds = 0);
        Task<List<ContentDeliveryRecord>> GetAllByStatusAsync(string status);
        Task<List<ContentDeliveryRecord>> GetAllByBackupRecordIdAsync(long id);
        Task<List<ContentDeliveryRecord>> GetAllByBackupRecordIdByStatusAsync(string resourceGroupId, long id, string status = "*");
        Task<ContentDeliveryRecord> GetByContentTypeByExecutionMessageAsync(string deliveryType, string executionMessage);
        Task<List<string>> GetAllNoneResponsiveAsync(List<string> statusChecks, int minuteDifference);
    }
}
