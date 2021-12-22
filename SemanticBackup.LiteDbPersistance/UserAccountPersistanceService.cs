using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.LiteDbPersistance
{
    public class UserAccountPersistanceService : IUserAccountPersistanceService
    {
        private LiteDatabaseAsync db;

        public UserAccountPersistanceService(ILiteDbContext context)
        {
            this.db = context.Database;
        }

        public async Task<List<UserAccount>> GetAllAsync()
        {
            return await db.GetCollection<UserAccount>().Query().OrderBy(x => x.FullName).ToListAsync();
        }
        public async Task<int> GetAllCountAsync()
        {
            return await db.GetCollection<UserAccount>().Query().Select(x => x.EmailAddress).CountAsync();
        }
        public async Task<bool> AddOrUpdateAsync(UserAccount record)
        {
            return await db.GetCollection<UserAccount>().UpsertAsync(record);
        }

        public async Task<UserAccount> GetByEmailAsync(string emailAddress)
        {
            return await db.GetCollection<UserAccount>().Query().Where(x => x.EmailAddress == emailAddress).FirstOrDefaultAsync();
        }
        public async Task<UserAccount> GetByCredentialsAsync(string emailAddress, string password)
        {
            return await db.GetCollection<UserAccount>().Query().Where(x => x.EmailAddress == emailAddress && x.Password == password).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveByEmailAsync(string emailAddress)
        {
            var collection = db.GetCollection<UserAccount>();
            var objFound = await collection.Query().Where(x => x.EmailAddress == emailAddress).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.EmailAddress));
            return false;
        }

        public async Task<bool> UpdateAsync(UserAccount record)
        {
            return await db.GetCollection<UserAccount>().UpdateAsync(record);
        }
    }
}
