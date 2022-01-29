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
        private LiteDatabaseAsync db;

        public DatabaseInfoPersistanceService(ILiteDbContext context)
        {
            this.db = context.Database;
        }

        public async Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId)
        {
            return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Select(x => x.Id).CountAsync();
        }
        public async Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record)
        {
            return await db.GetCollection<BackupDatabaseInfo>().UpsertAsync(record);
        }

        public async Task<BackupDatabaseInfo> GetByIdAsync(string id)
        {
            return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.Id == id).OrderBy(x => x.Name).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = db.GetCollection<BackupDatabaseInfo>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.Id));
            return false;
        }

        public async Task<bool> UpdateAsync(BackupDatabaseInfo record)
        {
            return await db.GetCollection<BackupDatabaseInfo>().UpdateAsync(record);
        }

        public async Task<BackupDatabaseInfo> GetByDatabaseNameAsync(string databaseName, string databaseType)
        {
            return await db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.DatabaseName != null && x.DatabaseType != null).Where(x => x.DatabaseName.Trim() == databaseName.Trim() && x.DatabaseType.Trim() == databaseType.Trim()).OrderBy(x => x.Name).FirstOrDefaultAsync();
        }
    }
}
