using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.LiteDbPersistance
{
    public class DatabaseInfoPersistanceService : IDatabaseInfoPersistanceService
    {
        private readonly ConnectionString connString;

        public DatabaseInfoPersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }

        public async Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.Name).ToListAsync();
            }
        }
        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Select(x => x.Id).CountAsync();
            }
        }
        public async Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupDatabaseInfo>().UpsertAsync(record);
            }
        }

        public async Task<BackupDatabaseInfo> GetByIdAsync(string id)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.Id == id).OrderBy(x => x.Name).FirstOrDefaultAsync();
            }
        }

        public async Task<bool> RemoveAsync(string id)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                var collection = db.GetCollection<BackupDatabaseInfo>();
                var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
                if (objFound != null)
                    return await collection.DeleteAsync(new BsonValue(objFound.Id));
                return false;
            }
        }

        public async Task<bool> UpdateAsync(BackupDatabaseInfo record)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupDatabaseInfo>().UpdateAsync(record);
            }
        }
    }
}
