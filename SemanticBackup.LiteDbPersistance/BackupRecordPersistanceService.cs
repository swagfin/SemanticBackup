using LiteDB;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupRecordPersistanceService : IBackupRecordPersistanceService
    {
        private readonly ConnectionString connString;
        private readonly IEnumerable<IRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;

        public BackupRecordPersistanceService(LiteDbPersistanceOptions options, IEnumerable<IRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers)
        {
            this.connString = options.ConnectionStringLiteDb;
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
        }

        public List<BackupRecord> GetAll(string resourcegroup)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllByStatus(string status)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllExpired()
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ExpiryDateUTC != null).Where(x => x.ExpiryDateUTC <= DateTime.UtcNow).OrderBy(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllByRegisteredDateByStatus(string resourcegroup, DateTime fromDate, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                if (status == "*")
                    return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToList();
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllByStatusUpdateDateByStatus(string resourcegroup, DateTime fromDate, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                if (status == "*")
                    return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToList();
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllByDatabaseIdByStatus(string resourcegroup, string id, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                if (status == "*")
                    return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToList();
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<BackupRecord> GetAllByDatabaseId(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToList();
            }
        }
        public List<string> GetAllNoneResponsiveIds(List<string> statusChecks, int minuteDifference)
        {
            if (statusChecks == null || statusChecks.Count == 0)
                return null;
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => statusChecks.Contains(x.BackupStatus)).Select(x => new { x.Id, x.BackupStatus, x.StatusUpdateDateUTC, x.RegisteredDateUTC }).ToList().Where(x => (DateTime.UtcNow - x.StatusUpdateDateUTC).TotalMinutes >= minuteDifference).Select(x => x.Id).ToList();
            }
        }
        public bool AddOrUpdate(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                bool savedSuccess = db.GetCollection<BackupRecord>().Upsert(record);
                if (savedSuccess)
                    this.DispatchUpdatedStatus(record, true);
                return savedSuccess;
            }
        }

        public bool UpdateStatusFeed(string id, string status, string message = null, long executionInMilliseconds = 0, string updateFilePath = null)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    objFound.BackupStatus = status;
                    objFound.StatusUpdateDateUTC = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(message))
                    {
                        objFound.ExecutionMessage = message;
                        objFound.ExecutionMilliseconds = $"{executionInMilliseconds:N2}ms";
                    }
                    if (!string.IsNullOrEmpty(updateFilePath))
                    {
                        objFound.Path = updateFilePath.Trim();
                    }
                    bool updatedSuccess = collection.Update(objFound);
                    if (updatedSuccess)
                        this.DispatchUpdatedStatus(objFound, false);
                    return updatedSuccess;
                }
                return false;
            }
        }

        public BackupRecord GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    string pathToRemove = objFound.Path;
                    bool removedSuccess = collection.Delete(new BsonValue(objFound.Id));
                    if (removedSuccess)
                        TryDeleteOldFile(pathToRemove);
                    return removedSuccess;
                }
                return false;
            }
        }

        public bool Update(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Update(record);
            }
        }

        private void DispatchUpdatedStatus(BackupRecord record, bool isNewRecord = false)
        {
            if (_backupRecordStatusChangedNotifiers != null)
                foreach (var notifier in _backupRecordStatusChangedNotifiers)
                    try
                    {
                        notifier.DispatchBackupRecordUpdatedStatus(record, isNewRecord);
                    }
                    catch { }
        }

        public List<BackupRecord> GetAllReadyAndPendingDelivery()
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<BackupRecord>().Query().Where(x => !x.ExecutedDeliveryRun && x.BackupStatus == BackupRecordBackupStatus.READY.ToString()).OrderBy(x => x.RegisteredDateUTC).ToList();
            }
        }
        public bool UpdateDeliveryRunned(string backupRecordId, bool hasRun, string executedDeliveryRunStatus)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == backupRecordId).FirstOrDefault();
                if (objFound != null)
                {
                    objFound.ExecutedDeliveryRun = hasRun;
                    objFound.ExecutedDeliveryRunStatus = executedDeliveryRunStatus;
                    bool updatedSuccess = collection.Update(objFound);
                    if (updatedSuccess)
                        this.DispatchUpdatedStatus(objFound, false);
                    return updatedSuccess;
                }
                return false;
            }
        }

        private void TryDeleteOldFile(string path)
        {
            try
            {
                bool success = false;
                int attempts = 0;
                do
                {
                    try
                    {
                        attempts++;
                        if (File.Exists(path))
                            File.Delete(path);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        if (attempts >= 10)
                        {
                            Thread.Sleep(2000);
                            throw new Exception($"Maximum Deletion Attempts, Error: {ex.Message}");
                        }
                    }
                }
                while (!success);

            }
            catch (Exception) { }
        }

    }
}
