using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class BackupScheduleRepositoryLiteDb : IBackupScheduleRepository
    {
        private readonly LiteDatabaseAsync _db;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        public BackupScheduleRepositoryLiteDb(IDatabaseInfoRepository databaseInfoRepository)
        {
            this._db = new LiteDatabaseAsync(new ConnectionString(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "schedules.db")) { Connection = ConnectionType.Shared });
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
            this._databaseInfoRepository = databaseInfoRepository;
        }

        public async Task<List<BackupSchedule>> GetAllAsync(string resourceGroupId)
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            return await _db.GetCollection<BackupSchedule>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId)).OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            return await _db.GetCollection<BackupSchedule>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId)).Select(x => x.Id).CountAsync();
        }
        public async Task<List<BackupSchedule>> GetAllDueByDateAsync()
        {
            return await _db.GetCollection<BackupSchedule>().Query().Where(x => x.NextRunUTC <= DateTime.UtcNow).OrderBy(x => x.NextRunUTC).ToListAsync();
        }
        public async Task<List<BackupSchedule>> GetAllByDatabaseIdAsync(string id)
        {
            return await _db.GetCollection<BackupSchedule>().Query().Where(x => x.BackupDatabaseInfoId == id).OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<bool> AddOrUpdateAsync(BackupSchedule record)
        {

            return await _db.GetCollection<BackupSchedule>().UpsertAsync(record);
        }

        public async Task<BackupSchedule> GetByIdAsync(string id)
        {
            return await _db.GetCollection<BackupSchedule>().Query().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = _db.GetCollection<BackupSchedule>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.Id));
            return false;
        }

        public async Task<bool> UpdateLastRunAsync(string id, DateTime lastRunUTC)
        {
            var collection = _db.GetCollection<BackupSchedule>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.LastRunUTC = lastRunUTC;
                return await collection.UpdateAsync(objFound);
            }
            return false;
        }
        public async Task<bool> UpdateAsync(BackupSchedule record)
        {
            return await _db.GetCollection<BackupSchedule>().UpdateAsync(record);
        }
    }
}
