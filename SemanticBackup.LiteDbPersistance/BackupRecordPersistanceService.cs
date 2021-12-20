﻿using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupRecordPersistanceService : IBackupRecordPersistanceService
    {
        private LiteDatabaseAsync db;
        private readonly IEnumerable<IRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;
        private readonly IContentDeliveryRecordPersistanceService _contentDeliveryRecordPersistanceService;

        public BackupRecordPersistanceService(
            ILiteDbContext context,
            IEnumerable<IRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers,
            IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService)
        {
            this.db = context.Database;
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
        }

        public async Task<List<BackupRecord>> GetAllAsync(string resourcegroup)
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<int> GetAllCountAsync(string resourcegroup)
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Select(x => x.RegisteredDateUTC).CountAsync();
        }
        public async Task<List<BackupRecord>> GetAllByStatusAsync(string status)
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllExpiredAsync()
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ExpiryDateUTC != null).Where(x => x.ExpiryDateUTC <= DateTime.UtcNow).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByRegisteredDateByStatusAsync(string resourcegroup, DateTime fromDate, string status = "*")
        {
            if (status == "*")
                return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByStatusUpdateDateByStatusAsync(string resourcegroup, DateTime fromDate, string status = "*")
        {
            if (status == "*")
                return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByDatabaseIdByStatusAsync(string resourcegroup, string id, string status = "*")
        {
            if (status == "*")
                return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.ResourceGroupId == resourcegroup).Where(x => x.BackupStatus == status && x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByDatabaseIdAsync(string id)
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<List<string>> GetAllNoneResponsiveIdsAsync(List<string> statusChecks, int minuteDifference)
        {
            if (statusChecks == null || statusChecks.Count == 0)
                return null;
            var records = await db.GetCollection<BackupRecord>().Query().Where(x => statusChecks.Contains(x.BackupStatus)).Select(x => new { x.Id, x.BackupStatus, x.StatusUpdateDateUTC, x.RegisteredDateUTC }).ToListAsync();
            return records.Where(x => (DateTime.UtcNow - x.StatusUpdateDateUTC).TotalMinutes >= minuteDifference).Select(x => x.Id).ToList();
        }
        public async Task<bool> AddOrUpdateAsync(BackupRecord record)
        {
            bool savedSuccess = await db.GetCollection<BackupRecord>().UpsertAsync(record);
            if (savedSuccess)
                this.DispatchUpdatedStatus(record, true);
            return savedSuccess;
        }

        public async Task<bool> UpdateStatusFeedAsync(string id, string status, string message = null, long executionInMilliseconds = 0, string updateFilePath = null)
        {
            var collection = db.GetCollection<BackupRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
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
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                if (updatedSuccess)
                    this.DispatchUpdatedStatus(objFound, false);
                return updatedSuccess;
            }
            return false;
        }

        public async Task<BackupRecord> GetByIdAsync(string id)
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = db.GetCollection<BackupRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                string pathToRemove = objFound.Path;
                bool removedSuccess = await collection.DeleteAsync(new BsonValue(objFound.Id));
                if (removedSuccess)
                {
                    TryDeleteContentDispatchRecordsAsync(id);
                    TryDeleteOldFile(pathToRemove);
                }
                return removedSuccess;
            }
            return false;
        }

        private async void TryDeleteContentDispatchRecordsAsync(string id)
        {
            try
            {
                //Get all by Backup Record
                List<ContentDeliveryRecord> associateRecords = await _contentDeliveryRecordPersistanceService.GetAllByBackupRecordIdAsync(id);
                if (associateRecords != null && associateRecords.Count > 0)
                    foreach (ContentDeliveryRecord record in associateRecords)
                        await _contentDeliveryRecordPersistanceService.RemoveAsync(record.Id);
            }
            catch { }
        }

        public async Task<bool> UpdateAsync(BackupRecord record)
        {

            return await db.GetCollection<BackupRecord>().UpdateAsync(record);
        }

        public async Task<List<BackupRecord>> GetAllReadyAndPendingDeliveryAsync()
        {
            return await db.GetCollection<BackupRecord>().Query().Where(x => !x.ExecutedDeliveryRun && x.BackupStatus == BackupRecordBackupStatus.READY.ToString()).OrderBy(x => x.RegisteredDateUTC).ToListAsync();
        }
        public async Task<bool> UpdateDeliveryRunnedAsync(string backupRecordId, bool hasRun, string executedDeliveryRunStatus)
        {
            var collection = db.GetCollection<BackupRecord>();
            var objFound = await collection.Query().Where(x => x.Id == backupRecordId).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.ExecutedDeliveryRun = hasRun;
                objFound.ExecutedDeliveryRunStatus = executedDeliveryRunStatus;
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                if (updatedSuccess)
                    this.DispatchUpdatedStatus(objFound, false);
                return updatedSuccess;
            }
            return false;
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
