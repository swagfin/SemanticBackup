using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Logic
{
    public class UserAccountRepositoryLiteDb : IUserAccountRepository
    {
        private readonly LiteDatabaseAsync _db;

        public UserAccountRepositoryLiteDb()
        {
#if DEBUG
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "user-accounts.dev.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#else
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "user-accounts.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#endif
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
        }

        public async Task<List<UserAccount>> GetAllAsync()
        {
            return await _db.GetCollection<UserAccount>().Query().OrderBy(x => x.FullName).ToListAsync();
        }
        public async Task<int> GetAllCountAsync()
        {
            return await _db.GetCollection<UserAccount>().Query().Select(x => x.EmailAddress).CountAsync();
        }
        public async Task<bool> AddOrUpdateAsync(UserAccount record)
        {
            return await _db.GetCollection<UserAccount>().UpsertAsync(record);
        }

        public async Task<UserAccount> GetByEmailAsync(string emailAddress)
        {
            return await _db.GetCollection<UserAccount>().Query().Where(x => x.EmailAddress == emailAddress).FirstOrDefaultAsync();
        }
        public async Task<UserAccount> GetByCredentialsAsync(string emailAddress, string password)
        {
            //Ensure Default Account Exist
            await EnsureAccountsExistsAsync();
            return await _db.GetCollection<UserAccount>().Query().Where(x => x.EmailAddress == emailAddress && x.Password == password).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveByEmailAsync(string emailAddress)
        {
            var collection = _db.GetCollection<UserAccount>();
            var objFound = await collection.Query().Where(x => x.EmailAddress == emailAddress).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.EmailAddress));
            return false;
        }

        public async Task<bool> UpdateLastSeenAsync(string emailAddress, DateTime lastSeenUTC, string lastToken)
        {
            var collection = _db.GetCollection<UserAccount>();
            var objFound = await collection.Query().Where(x => x.EmailAddress == emailAddress).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.LastLoginUTC = lastSeenUTC;
                objFound.LastLoginToken = lastToken;
                return await collection.UpdateAsync(objFound);
            }
            return false;
        }
        public async Task<bool> UpdateAsync(UserAccount record)
        {
            return await _db.GetCollection<UserAccount>().UpdateAsync(record);
        }

        private async Task EnsureAccountsExistsAsync()
        {
            //User Account Service
            int count = await GetAllCountAsync();
            if (count == 0)
                await AddOrUpdateAsync(new Core.Models.UserAccount { EmailAddress = "admin@admin.com", FullName = "Administrator", Password = "admin", UserAccountType = Core.Models.UserAccountType.ADMIN });
        }
    }
}
