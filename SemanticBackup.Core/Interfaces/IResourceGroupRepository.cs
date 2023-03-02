using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IResourceGroupRepository
    {
        Task<List<ResourceGroup>> GetAllAsync();
        Task<ResourceGroup> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> SwitchAsync(string id);
        Task<bool> AddAsync(ResourceGroup record);
        Task<bool> UpdateAsync(ResourceGroup record);
    }
}
