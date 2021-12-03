﻿using LiteDB;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupRecordPersistanceService : IBackupRecordPersistanceService
    {
        private readonly ConnectionString connString;
        private readonly IEnumerable<IBackupRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;

        public BackupRecordPersistanceService(LiteDbPersistanceOptions options, IEnumerable<IBackupRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers)
        {
            this.connString = options.ConnectionStringLiteDb;
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
        }

        public List<BackupRecord> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().OrderByDescending(x => x.RegisteredDate).ToList();
            }
        }
        public List<BackupRecord> GetAllByStatus(string status)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status).OrderByDescending(x => x.RegisteredDate).ToList();
            }
        }
        public List<BackupRecord> GetAllExpired(DateTime currentDate)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().Where(x => x.ExpiryDate != null).Where(x => x.ExpiryDate <= currentDate).OrderBy(x => x.RegisteredDate).ToList();
            }
        }
        public List<BackupRecord> GetAllByRegisteredDateByStatus(DateTime fromDate, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                if (status == "*")
                    return db.GetCollection<BackupRecord>().Query().Where(x => x.RegisteredDate > fromDate).OrderByDescending(x => x.RegisteredDate).ToList();
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status && x.RegisteredDate > fromDate).OrderByDescending(x => x.RegisteredDate).ToList();
            }
        }
        public List<BackupRecord> GetAllByStatusUpdateDateByStatus(DateTime fromDate, string status = "*")
        {
            using (var db = new LiteDatabase(connString))
            {
                if (status == "*")
                    return db.GetCollection<BackupRecord>().Query().Where(x => x.StatusUpdateDate > fromDate).OrderByDescending(x => x.RegisteredDate).ToList();
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status && x.StatusUpdateDate > fromDate).OrderByDescending(x => x.RegisteredDate).ToList();
            }
        }
        public List<BackupRecord> GetAllByDatabaseId(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDate).ToList();
            }
        }

        public bool AddOrUpdate(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                bool savedSuccess = db.GetCollection<BackupRecord>().Upsert(record);
                if (savedSuccess)
                    this.DispatchUpdatedStatus(record, true);
                return savedSuccess;
            }
        }

        public bool UpdateStatusFeed(string id, string status, DateTime updateDate, string message = null, long executionInMilliseconds = 0, string updateFilePath = null)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    objFound.BackupStatus = status;
                    objFound.StatusUpdateDate = updateDate;
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
                return db.GetCollection<BackupRecord>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                {
                    return collection.Delete(new BsonValue(objFound.Id));
                }
                return false;
            }
        }

        public bool Update(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Update(record);
            }
        }

        private void DispatchUpdatedStatus(BackupRecord record, bool isNewRecord = false)
        {
            if (_backupRecordStatusChangedNotifiers != null)
                foreach (var notifier in _backupRecordStatusChangedNotifiers)
                    try
                    {
                        notifier.DispatchUpdatedStatus(record, isNewRecord);
                    }
                    catch { }
        }
    }
}
