﻿using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Logic
{
    public class ContentDeliveryRecordRepositoryLiteDb : IContentDeliveryRecordRepository
    {
        private readonly LiteDatabaseAsync _db;
        private readonly IEnumerable<IRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;

        public ContentDeliveryRecordRepositoryLiteDb(IEnumerable<IRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers)
        {
#if DEBUG
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "backup-delivery-history.dev.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#else
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "backup-delivery-history.db")) { Password = "12345678", Connection = ConnectionType.Shared });
#endif
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
            //Proceed
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
        }

        public async Task<List<ContentDeliveryRecord>> GetAllAsync(string resourceGroupId)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<ContentDeliveryRecord>> GetAllByStatusAsync(string status)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.CurrentStatus == status).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<ContentDeliveryRecord>> GetAllByBackupRecordIdByStatusAsync(string resourceGroupId, string id, string status = "*")
        {
            if (status == "*")
                return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Where(x => x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.ResourceGroupId == resourceGroupId).Where(x => x.CurrentStatus == status && x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<ContentDeliveryRecord>> GetAllByBackupRecordIdAsync(string id)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.BackupRecordId == id).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
        }

        public async Task<bool> AddOrUpdateAsync(ContentDeliveryRecord record)
        {
            var collection = _db.GetCollection<ContentDeliveryRecord>();
            var objFound = await collection.Query().Where(x => x.Id == record.Id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.StatusUpdateDateUTC = record.StatusUpdateDateUTC;
                objFound.CurrentStatus = record.CurrentStatus;
                objFound.ExecutionMessage = record.ExecutionMessage;
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                if (updatedSuccess)
                    this.DispatchUpdatedStatus(objFound, false);
                return updatedSuccess;
            }
            else
            {
                bool savedSuccess = await _db.GetCollection<ContentDeliveryRecord>().UpsertAsync(record);
                if (savedSuccess)
                    this.DispatchUpdatedStatus(record, true);
                return savedSuccess;
            }
        }

        public async Task<bool> UpdateStatusFeedAsync(string id, string status, string message = null, long executionInMilliseconds = 0)
        {
            var collection = _db.GetCollection<ContentDeliveryRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.CurrentStatus = status;
                objFound.StatusUpdateDateUTC = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(message))
                {
                    objFound.ExecutionMessage = message;
                    objFound.ExecutionMilliseconds = $"{executionInMilliseconds:N2}ms";
                }
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                if (updatedSuccess)
                    this.DispatchUpdatedStatus(objFound, false);
                return updatedSuccess;
            }
            return false;
        }

        public async Task<ContentDeliveryRecord> GetByIdAsync(string id)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = _db.GetCollection<ContentDeliveryRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                return await collection.DeleteAsync(new BsonValue(objFound.Id));
            }
            return false;
        }
        public async Task<bool> UpdateAsync(ContentDeliveryRecord record)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().UpdateAsync(record);
        }

        public async Task<ContentDeliveryRecord> GetByContentTypeByExecutionMessageAsync(string deliveryType, string executionMessage)
        {
            return await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => (x.DeliveryType == deliveryType && x.ExecutionMessage == executionMessage) || (x.DeliveryType == deliveryType && x.BackupRecordId == executionMessage)).FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAllNoneResponsiveAsync(List<string> statusChecks, int minuteDifference)
        {
            if (statusChecks == null || statusChecks.Count == 0)
                return null;
            var records = await _db.GetCollection<ContentDeliveryRecord>().Query().Where(x => statusChecks.Contains(x.CurrentStatus)).OrderBy(x => x.RegisteredDateUTC).Select(x => new { x.Id, x.CurrentStatus, x.StatusUpdateDateUTC, x.RegisteredDateUTC }).ToListAsync();
            return records.Where(x => (DateTime.UtcNow - x.StatusUpdateDateUTC).TotalMinutes >= minuteDifference).Select(x => x.Id).ToList();
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

    }
}