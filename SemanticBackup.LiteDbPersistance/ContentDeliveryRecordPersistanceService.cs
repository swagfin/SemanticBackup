using LiteDB;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.LiteDbPersistance
{
    public class ContentDeliveryRecordPersistanceService : IContentDeliveryRecordPersistanceService
    {
        private readonly ConnectionString connString;
        private readonly IEnumerable<IRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;

        public ContentDeliveryRecordPersistanceService(LiteDbPersistanceOptions options, IEnumerable<IRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers)
        {
            this.connString = options.ConnectionStringLiteDb;
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
        }

        public List<ContentDeliveryRecord> GetAll(string resourceGroupId)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<ContentDeliveryRecord> GetAllByStatus(string status)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.CurrentStatus == status).OrderBy(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<ContentDeliveryRecord> GetAllByBackupRecordIdByStatus(string resourceGroupId, string id, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                if (status == "*")
                    return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Where(x => x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToList();
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Where(x => x.CurrentStatus == status && x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<ContentDeliveryRecord> GetAllByBackupRecordId(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToList();
            }
        }

        public bool AddOrUpdate(ContentDeliveryRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ContentDeliveryRecord>();
                var objFound = collection.Query().Where(x => x.Id == record.Id).FirstOrDefault();
                if (objFound != null)
                {
                    objFound.StatusUpdateDateUTC = record.StatusUpdateDateUTC;
                    objFound.CurrentStatus = record.CurrentStatus;
                    objFound.ExecutionMessage = record.ExecutionMessage;
                    bool updatedSuccess = collection.Update(objFound);
                    if (updatedSuccess)
                        this.DispatchUpdatedStatus(objFound, false);
                    return updatedSuccess;
                }
                else
                {
                    bool savedSuccess = db.GetCollection<ContentDeliveryRecord>().Upsert(record);
                    if (savedSuccess)
                        this.DispatchUpdatedStatus(record, true);
                    return savedSuccess;
                }
            }
        }

        public bool UpdateStatusFeed(string id, string status, string message = null, long executionInMilliseconds = 0)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ContentDeliveryRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    objFound.CurrentStatus = status;
                    objFound.StatusUpdateDateUTC = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(message))
                    {
                        objFound.ExecutionMessage = message;
                        objFound.ExecutionMilliseconds = $"{executionInMilliseconds:N2}ms";
                    }
                    bool updatedSuccess = collection.Update(objFound);
                    if (updatedSuccess)
                        this.DispatchUpdatedStatus(objFound, false);
                    return updatedSuccess;
                }
                return false;
            }
        }

        public ContentDeliveryRecord GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ContentDeliveryRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    return collection.Delete(new BsonValue(objFound.Id));
                }
                return false;
            }
        }

        public bool Update(ContentDeliveryRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Update(record);
            }
        }

        private void DispatchUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord = false)
        {
            if (_backupRecordStatusChangedNotifiers != null)
                foreach (var notifier in _backupRecordStatusChangedNotifiers)
                    try
                    {
                        notifier.DispatchContentDeliveryUpdatedStatus(record, isNewRecord);
                    }
                    catch { }
        }

        public ContentDeliveryRecord GetByContentTypeByExecutionMessage(string deliveryType, string executionMessage)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.DeliveryType == deliveryType && x.ExecutionMessage == executionMessage).FirstOrDefault();
            }
        }

        public List<string> GetAllNoneResponsive(List<string> statusChecks, int minuteDifference)
        {
            if (statusChecks == null || statusChecks.Count == 0)
                return null;
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryRecord>().Query().Where(x => statusChecks.Contains(x.CurrentStatus)).OrderBy(x => x.RegisteredDateUTC).Select(x => new { x.Id, x.CurrentStatus, x.StatusUpdateDateUTC, x.RegisteredDateUTC }).ToList().Where(x => (DateTime.UtcNow - x.StatusUpdateDateUTC).TotalMinutes >= minuteDifference).Select(x => x.Id).ToList();
            }
        }
    }
}
