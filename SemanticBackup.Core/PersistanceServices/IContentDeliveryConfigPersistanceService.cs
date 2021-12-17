using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IContentDeliveryConfigPersistanceService
    {
        Task<List<ContentDeliveryConfiguration>> GetAllAsync(string resourceGroupId);
        Task<ContentDeliveryConfiguration> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> AddOrUpdateAsync(ContentDeliveryConfiguration record);
        Task<bool> UpdateAsync(ContentDeliveryConfiguration record);
        Task<bool> AddOrUpdateAsync(List<ContentDeliveryConfiguration> records);
        Task<bool> RemoveAllByResourceGroupAsync(string resourceGroupId);
    }
}
