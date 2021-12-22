using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IUserAccountPersistanceService
    {
        Task<List<UserAccount>> GetAllAsync();
        Task<UserAccount> GetByEmailAsync(string emailAddress);
        Task<UserAccount> GetByCredentialsAsync(string emailAddress, string password);
        Task<bool> RemoveByEmailAsync(string emailAddress);
        Task<bool> AddOrUpdateAsync(UserAccount record);
        Task<bool> UpdateAsync(UserAccount record);
        Task<int> GetAllCountAsync();
    }
}
