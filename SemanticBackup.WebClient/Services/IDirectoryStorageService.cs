using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services
{
    public interface IDirectoryStorageService
    {
        Task ReloadTempDirectories(List<ActiveDirectoryResponse> activeDirectoryResponses = null);
        Task<bool> AddAsync(ActiveDirectoryRequest apiDirectory);
        Task<List<ActiveDirectoryResponse>> GetAllAsync();
        Task<ActiveDirectoryResponse> GetByIdAsync(string id);
        Task<bool> RemoveAsync(string id);
        Task<bool> SwitchAsync(string id);
        Task<bool> UpdateAsync(ActiveDirectoryRequest apiDirectory);
    }
}
