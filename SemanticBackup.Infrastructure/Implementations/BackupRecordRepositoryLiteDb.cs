using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core;
using SemanticBackup.Core.Helpers;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class BackupRecordRepositoryLiteDb : IBackupRecordRepository
    {
        private readonly LiteDatabaseAsync _db;
        private readonly IEnumerable<IRecordStatusChangedNotifier> _backupRecordStatusChangedNotifiers;
        private readonly IContentDeliveryRecordRepository _contentDeliveryRecordPersistanceService;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        public BackupRecordRepositoryLiteDb(IEnumerable<IRecordStatusChangedNotifier> backupRecordStatusChangedNotifiers, IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService, IDatabaseInfoRepository databaseInfoRepository)
        {
            this._db = new LiteDatabaseAsync(new ConnectionString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "backups.db")) { Connection = ConnectionType.Shared });
            //Init
            this._db.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
            //Proceed
            this._backupRecordStatusChangedNotifiers = backupRecordStatusChangedNotifiers;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
            this._databaseInfoRepository = databaseInfoRepository;
        }


        public async Task<List<BackupRecord>> GetAllAsync(string resourceGroupId)
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId)).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId)).Select(x => x.Id).CountAsync();
        }
        public async Task<List<BackupRecord>> GetAllByStatusAsync(string status)
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByRestoreStatusAsync(string status)
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.RestoreStatus == status).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllExpiredAsync()
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.ExpiryDateUTC <= DateTime.UtcNow).OrderBy(x => x.Id).ToListAsync();
        }
        public async Task<bool> UpdateExpiryDateByIdAsync(long id, DateTime expiryDateUtc)
        {
            BackupRecord existingRecord = await GetByIdAsync(id);
            if (existingRecord == null)
                return false;
            existingRecord.ExpiryDateUTC = expiryDateUtc;
            return await UpdateAsync(existingRecord);
        }
        public async Task<List<BackupRecord>> GetAllByRegisteredDateByStatusAsync(string resourceGroupId, DateTime fromDate, string status = "*")
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            if (status == "*")
                return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId) && x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.Id).ToListAsync();
            return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId) && x.BackupStatus == status && x.RegisteredDateUTC > fromDate).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByStatusUpdateDateByStatusAsync(string resourceGroupId, DateTime fromDate, string status = "*")
        {
            List<string> dbCollection = await _databaseInfoRepository.GetDatabaseIdsForResourceGroupAsync(resourceGroupId);
            if (status == "*")
                return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId) && x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.Id).ToListAsync();
            return await _db.GetCollection<BackupRecord>().Query().Where(x => dbCollection.Contains(x.BackupDatabaseInfoId) && x.BackupStatus == status && x.StatusUpdateDateUTC > fromDate).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByDatabaseIdByStatusAsync(string databaseId, string status = "*")
        {
            if (status == "*")
                return await _db.GetCollection<BackupRecord>().Query().Where(x => x.BackupDatabaseInfoId == databaseId).OrderByDescending(x => x.Id).ToListAsync();
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.BackupStatus == status && x.BackupDatabaseInfoId == databaseId).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<BackupRecord>> GetAllByDatabaseIdAsync(string id)
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.BackupDatabaseInfoId == id).OrderByDescending(x => x.Id).ToListAsync();
        }
        public async Task<List<long>> GetAllNoneResponsiveIdsAsync(List<string> statusChecks, int minuteDifference)
        {
            if (statusChecks == null || statusChecks.Count == 0)
                return null;
            var records = await _db.GetCollection<BackupRecord>().Query().Where(x => statusChecks.Contains(x.BackupStatus)).Select(x => new { x.Id, x.BackupStatus, x.StatusUpdateDateUTC, x.RegisteredDateUTC }).ToListAsync();
            return records.Where(x => (DateTime.UtcNow - x.StatusUpdateDateUTC).TotalMinutes >= minuteDifference).Select(x => x.Id).ToList();
        }
        public async Task<bool> AddOrUpdateAsync(BackupRecord record)
        {
            bool savedSuccess = await _db.GetCollection<BackupRecord>().UpsertAsync(record);
            if (savedSuccess)
                this.DispatchUpdatedStatus(record, true);
            return savedSuccess;
        }

        public async Task<bool> UpdateStatusFeedAsync(long id, string status, string message = null, long executionInMilliseconds = 0, string updateFilePath = null)
        {
            var collection = _db.GetCollection<BackupRecord>();
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

        public async Task<bool> UpdateRestoreStatusFeedAsync(long id, string status, string message = null, string confirmationToken = null)
        {
            var collection = _db.GetCollection<BackupRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                objFound.RestoreStatus = status;
                if (!string.IsNullOrEmpty(message))
                    objFound.RestoreExecutionMessage = message;
                if (!string.IsNullOrEmpty(confirmationToken))
                    objFound.RestoreConfirmationToken = confirmationToken;
                bool updatedSuccess = await collection.UpdateAsync(objFound);
                if (updatedSuccess)
                    this.DispatchUpdatedStatus(objFound, false);
                return updatedSuccess;
            }
            return false;
        }
        public async Task<BackupRecord> GetByIdAsync(long id)
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => x.Id == id).FirstOrDefaultAsync();
        }
        public async Task<BackupRecord> VerifyBackupRecordInResourceGroupThrowIfNotExistAsync(string resourceGroupId, long backupRecordId)
        {
            BackupRecord backupRecordResponse = await _db.GetCollection<BackupRecord>().Query().Where(x => x.Id == backupRecordId).FirstOrDefaultAsync() ?? throw new Exception($"unknown backup record with identity key {backupRecordId} under resource group id: {resourceGroupId}");
            //retrive the Database Information
            _ = await _databaseInfoRepository.VerifyDatabaseInResourceGroupThrowIfNotExistAsync(resourceGroupId, backupRecordResponse.BackupDatabaseInfoId ?? string.Empty);
            return backupRecordResponse;
        }

        public async Task RemoveWithFileAsync(long id)
        {
            var collection = _db.GetCollection<BackupRecord>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
            {
                string pathToRemove = objFound.Path;
                await collection.DeleteAsync(new BsonValue(objFound.Id));
                //remove content dispatch records
                await TryDeleteContentDispatchRecordsAsync(id);
                await TryDeleteOldFileAsync(pathToRemove);
            }
        }

        private async Task TryDeleteContentDispatchRecordsAsync(long id)
        {
            try
            {
                //Get all by Backup Record
                List<BackupRecordDelivery> associateRecords = await _contentDeliveryRecordPersistanceService.GetAllByBackupRecordIdAsync(id);
                if (associateRecords != null && associateRecords.Count > 0)
                    foreach (BackupRecordDelivery record in associateRecords)
                        await _contentDeliveryRecordPersistanceService.RemoveAsync(record.Id);
            }
            catch { }
        }

        public async Task<bool> UpdateAsync(BackupRecord record)
        {

            return await _db.GetCollection<BackupRecord>().UpdateAsync(record);
        }

        public async Task<List<BackupRecord>> GetAllReadyAndPendingDeliveryAsync()
        {
            return await _db.GetCollection<BackupRecord>().Query().Where(x => !x.ExecutedDeliveryRun && x.BackupStatus == BackupRecordStatus.READY.ToString()).OrderBy(x => x.Id).ToListAsync();
        }
        public async Task<bool> UpdateDeliveryRunnedAsync(long backupRecordId, bool hasRun, string executedDeliveryRunStatus)
        {
            ILiteCollectionAsync<BackupRecord> collection = _db.GetCollection<BackupRecord>();
            BackupRecord objFound = await collection.Query().Where(x => x.Id == backupRecordId).FirstOrDefaultAsync();
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

        private static async Task TryDeleteOldFileAsync(string path)
        {
            try
            {
                await WithRetry.TaskAsync(() =>
                  {
                      //check file exists
                      if (File.Exists(path))
                          File.Delete(path);
                      return Task.CompletedTask;

                  }, 3, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex) { Console.WriteLine($"Failed to remove File: {ex.Message}"); }
        }
    }
}
