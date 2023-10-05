using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IResourceGroupRepository
    {
        Task<List<ResourceGroup>> GetAllAsync();
        Task<ResourceGroup> GetByIdOrKeyAsync(string resourceGroupIdentifier);
        Task<bool> RemoveAsync(string resourceGroupIdentifier);
        Task<bool> SwitchAsync(string resourceGroupIdentifier);
        Task<bool> AddAsync(ResourceGroup record);
        Task<bool> UpdateAsync(ResourceGroup record);
        Task<ResourceGroup> VerifyByIdOrKeyThrowIfNotExistAsync(string resourceGroupIdentifier);
    }
}
