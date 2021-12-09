using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services
{
    public interface IResourceGroupService
    {
        Task ReloadTempResourceGroups(List<ResourceGroupResponse> records = null);
        Task<bool> AddAsync(ResourceGroupRequest record);
        Task<List<ResourceGroupResponse>> GetAllAsync();
        Task<ResourceGroupResponse> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> SwitchAsync(string id);
        Task<bool> UpdateAsync(ResourceGroupRequest record);
    }
}
