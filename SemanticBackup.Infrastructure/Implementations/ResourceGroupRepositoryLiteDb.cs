﻿using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class ResourceGroupRepositoryLiteDb : IResourceGroupRepository
    {
        private LiteDatabaseAsync _db;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;
        private readonly IBackupScheduleRepository _backupSchedulePersistanceService;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public ResourceGroupRepositoryLiteDb(IBackupRecordRepository backupRecordPersistanceService, IBackupScheduleRepository backupSchedulePersistanceService, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            this._db = new LiteDatabaseAsync(new ConnectionString(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "resources.db")) { Connection = ConnectionType.Shared });
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
            //Proceed
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }
        public async Task<bool> AddAsync(ResourceGroup record)
        {
            record.Id = Guid.NewGuid().ToString().ToUpper();
            record.Name = record.Name.Trim();
            //attempt to check if exists
            ResourceGroup preExistingRecord = await GetByIdOrKeyAsync(record.Id);
            if (preExistingRecord != null)
                throw new Exception($"there is already an existing resource group with provided identity: {record.Name} or Key:  {record.Key}");
            return await _db.GetCollection<ResourceGroup>().UpsertAsync(record);
        }

        public async Task<List<ResourceGroup>> GetAllAsync()
        {
            return await _db.GetCollection<ResourceGroup>().Query().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<ResourceGroup> GetByIdOrKeyAsync(string resourceGroupIdentifier)
        {
            return await _db.GetCollection<ResourceGroup>().Query().Where(x => x.Id == resourceGroupIdentifier.Trim() || x.Key == resourceGroupIdentifier.Trim()).FirstOrDefaultAsync();
        }
        public async Task<ResourceGroup> VerifyByIdOrKeyThrowIfNotExistAsync(string resourceGroupIdentifier)
        {
            return await GetByIdOrKeyAsync(resourceGroupIdentifier) ?? throw new Exception($"resource group not found with provided identity: {resourceGroupIdentifier}");
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
        }
    }
}
