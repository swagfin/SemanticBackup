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
    public class DatabaseInfoRepositoryLiteDb : IDatabaseInfoRepository
    {
        private LiteDatabaseAsync _db;

        public DatabaseInfoRepositoryLiteDb()
        {
#if DEBUG
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database-infos.dev.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#else
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database-infos.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#endif
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
        }

        public async Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Select(x => x.Id).CountAsync();
        }
        public async Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().UpsertAsync(record);
        }

        public async Task<BackupDatabaseInfo> GetByIdAsync(string id)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.Id == id.Trim()).FirstOrDefaultAsync();
        }
        public async Task<BackupDatabaseInfo> VerifyDatabaseInResourceGroupThrowIfNotExistAsync(string resourceGroupId, string databaseIdentifier)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId && (x.Id == databaseIdentifier.Trim() || x.DatabaseName == databaseIdentifier.Trim())).FirstOrDefaultAsync() ?? throw new Exception($"unknown database with identity key {databaseIdentifier} under resource group id: {resourceGroupId}");
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = _db.GetCollection<BackupDatabaseInfo>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.Id));
            return false;
        }

        public async Task<bool> UpdateAsync(BackupDatabaseInfo record)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().UpdateAsync(record);
        }

        public async Task<BackupDatabaseInfo> GetByDatabaseNameAsync(string databaseName, string databaseType)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.DatabaseName != null && x.DatabaseType != null).Where(x => x.DatabaseName.Trim() == databaseName.Trim() && x.DatabaseType.Trim() == databaseType.Trim()).OrderBy(x => x.Name).FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetDatabaseIdsForResourceGroupAsync(string resourceGroupId)
        {
            return await _db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Select(x => x.Id).ToListAsync();
        }
    }
}
