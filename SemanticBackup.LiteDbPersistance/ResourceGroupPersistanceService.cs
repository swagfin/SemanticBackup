using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class ResourceGroupPersistanceService : IResourceGroupPersistanceService
    {
        private readonly ConnectionString connString;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IContentDeliveryConfigPersistanceService _contentDeliveryConfigPersistanceService;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;

        public ResourceGroupPersistanceService(LiteDbPersistanceOptions options, IBackupRecordPersistanceService backupRecordPersistanceService, IContentDeliveryConfigPersistanceService contentDeliveryConfigPersistanceService, IBackupSchedulePersistanceService backupSchedulePersistanceService, IDatabaseInfoPersistanceService databaseInfoPersistanceService)
        {
            this.connString = options.ConnectionStringLiteDb;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._contentDeliveryConfigPersistanceService = contentDeliveryConfigPersistanceService;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }
        public bool Add(ResourceGroup record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ResourceGroup>().Upsert(record);
            }
        }

        public List<ResourceGroup> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ResourceGroup>().Query().OrderBy(x => x.Name).ToList();
            }
        }

        public ResourceGroup GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ResourceGroup>().Query().Where(x => x.Id == id).OrderBy(x => x.Name).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ResourceGroup>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    bool success = collection.Delete(new BsonValue(objFound.Id));
                    TryDeleteAllResourcesForGroup(id);
                    return success;
                }
                return false;
            }
        }

        private void TryDeleteAllResourcesForGroup(string resourceGroupId)
        {
            //Delete Databases
            try
            {
                var associatedDatabases = _databaseInfoPersistanceService.GetAll(resourceGroupId);
                if (associatedDatabases != null)
                    foreach (var record in associatedDatabases)
                        _databaseInfoPersistanceService.Remove(record.Id);
            }
            catch { }
            //Delete Schedules
            try
            {
                var associatedSchedules = _backupSchedulePersistanceService.GetAll(resourceGroupId);
                if (associatedSchedules != null)
                    foreach (var record in associatedSchedules)
                        _backupSchedulePersistanceService.Remove(record.Id);
            }
            catch { }
            //Delete Backup Records
            try
            {
                var associatedBackupRecords = _backupRecordPersistanceService.GetAll(resourceGroupId);
                if (associatedBackupRecords != null)
                    foreach (var record in associatedBackupRecords)
                        _backupRecordPersistanceService.Remove(record.Id);
            }
            catch { }
            //Delete Configuration for Dispatch
            try
            {
                var associatedDeliveryConfigs = _contentDeliveryConfigPersistanceService.GetAll(resourceGroupId);
                if (associatedDeliveryConfigs != null)
                    foreach (var record in associatedDeliveryConfigs)
                        _contentDeliveryConfigPersistanceService.Remove(record.Id);
            }
            catch { }
        }

        public bool Switch(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ResourceGroup>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    if (long.TryParse(DateTime.UtcNow.ToString("yyyyMMddHHmmss"), out long lastAccess))
                    {
                        objFound.LastAccess = lastAccess;
                        bool updatedSuccess = collection.Update(objFound);
                        return updatedSuccess;
                    }
                return false;
            }

        }

        public bool Update(ResourceGroup record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ResourceGroup>().Update(record);
            }
        }
    }
}
