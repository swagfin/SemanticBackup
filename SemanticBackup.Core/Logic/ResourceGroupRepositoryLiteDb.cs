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
    public class ResourceGroupRepositoryLiteDb : IResourceGroupRepository
    {
        private LiteDatabaseAsync _db;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;
        private readonly IContentDeliveryConfigRepository _contentDeliveryConfigPersistanceService;
        private readonly IBackupScheduleRepository _backupSchedulePersistanceService;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public ResourceGroupRepositoryLiteDb(IBackupRecordRepository backupRecordPersistanceService, IContentDeliveryConfigRepository contentDeliveryConfigPersistanceService, IBackupScheduleRepository backupSchedulePersistanceService, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
#if DEBUG
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "resource-groups.dev.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#else
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "resource-groups.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#endif
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
            //Proceed
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._contentDeliveryConfigPersistanceService = contentDeliveryConfigPersistanceService;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }
        public async Task<bool> AddAsync(ResourceGroup record)
        {
            return await _db.GetCollection<ResourceGroup>().UpsertAsync(record);
        }

        public async Task<List<ResourceGroup>> GetAllAsync()
        {
            return await _db.GetCollection<ResourceGroup>().Query().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<ResourceGroup> GetByIdAsync(string id)
        {
            return await _db.GetCollection<ResourceGroup>().Query().Where(x => x.Id == id).OrderBy(x => x.Name).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = _db.GetCollection<ResourceGroup>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                bool success = await collection.DeleteAsync(new BsonValue(objFound.Id));
                await TryDeleteAllResourcesForGroupAsync(id);
                return success;
            }
            return false;
        }

        public async Task<bool> SwitchAsync(string id)
        {
            var collection = _db.GetCollection<ResourceGroup>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.LastAccess = DateTime.UtcNow.ConvertLongFormat();
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                return updatedSuccess;
            }
            return false;
        }

        public async Task<bool> UpdateAsync(ResourceGroup record)
        {
            return await _db.GetCollection<ResourceGroup>().UpdateAsync(record);
        }

        private async Task TryDeleteAllResourcesForGroupAsync(string resourceGroupId)
        {
            //Delete Databases
            try
            {
                var associatedDatabases = await _databaseInfoPersistanceService.GetAllAsync(resourceGroupId);
                if (associatedDatabases != null)
                    foreach (var record in associatedDatabases)
                        await _databaseInfoPersistanceService.RemoveAsync(record.Id);
            }
            catch { }
            //Delete Schedules
            try
            {
                var associatedSchedules = await _backupSchedulePersistanceService.GetAllAsync(resourceGroupId);
                if (associatedSchedules != null)
                    foreach (var record in associatedSchedules)
                        await _backupSchedulePersistanceService.RemoveAsync(record.Id);
            }
            catch { }
            //Delete Backup Records
            try
            {
                var associatedBackupRecords = await _backupRecordPersistanceService.GetAllAsync(resourceGroupId);
                if (associatedBackupRecords != null)
                    foreach (var record in associatedBackupRecords)
                        await _backupRecordPersistanceService.RemoveAsync(record.Id);
            }
            catch { }
            //Delete Configuration for Dispatch
            try
            {
                var associatedDeliveryConfigs = await _contentDeliveryConfigPersistanceService.GetAllAsync(resourceGroupId);
                if (associatedDeliveryConfigs != null)
                    foreach (var record in associatedDeliveryConfigs)
                        await _contentDeliveryConfigPersistanceService.RemoveAsync(record.Id);
            }
            catch { }
        }

    }
}
