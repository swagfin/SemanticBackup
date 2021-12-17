using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupSchedulePersistanceService : IBackupSchedulePersistanceService
    {
        private readonly ConnectionString connString;

        public BackupSchedulePersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }

        public async Task<List<BackupSchedule>> GetAllAsync(string resourcegroup)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().Query().Where(x => x.ResourceGroupId == resourcegroup).ToListAsync();
            }
        }
        public async Task<int> GetAllCountAsync(string resourcegroup)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().Query().Where(x => x.ResourceGroupId == resourcegroup).Select(x => x.Id).CountAsync();
            }
        }
        public async Task<List<BackupSchedule>> GetAllDueByDateAsync()
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().Query().Where(x => x.NextRunUTC <= DateTime.UtcNow && !string.IsNullOrWhiteSpace(x.ResourceGroupId)).OrderBy(x => x.NextRunUTC).ToListAsync();
            }
        }
        public async Task<List<BackupSchedule>> GetAllByDatabaseIdAsync(string id)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().Query().Where(x => x.BackupDatabaseInfoId == id && !string.IsNullOrWhiteSpace(x.ResourceGroupId)).ToListAsync();
            }
        }
        public async Task<bool> AddOrUpdateAsync(BackupSchedule record)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().UpsertAsync(record);
            }
        }

        public async Task<BackupSchedule> GetByIdAsync(string id)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            }
        }

        public async Task<bool> RemoveAsync(string id)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                var collection = db.GetCollection<BackupSchedule>();
                var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
                if (objFound != null)
                    return await collection.DeleteAsync(new BsonValue(objFound.Id));
                return false;
            }
        }

        public async Task<bool> UpdateAsync(BackupSchedule record)
        {
            using (var db = new LiteDatabaseAsync(connString))
            {
                await db.PragmaAsync("UTC_DATE", true);
                return await db.GetCollection<BackupSchedule>().UpsertAsync(record);
            }
        }
    }
}
