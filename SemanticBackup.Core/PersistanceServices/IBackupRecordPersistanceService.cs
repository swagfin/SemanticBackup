using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupRecordPersistanceService
    {
        List<BackupRecord> GetAll(string resourceGroupId);
        BackupRecord GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupRecord record);
        bool Update(BackupRecord record);
        bool UpdateStatusFeed(string id, string status, string message = null, long executionInMilliseconds = 0, string newFilePath = null);
        List<BackupRecord> GetAllByStatus(string status);
        List<BackupRecord> GetAllByDatabaseId(string id);
        List<BackupRecord> GetAllByRegisteredDateByStatus(string resourceGroupId, DateTime fromDate, string status = "*");
        List<BackupRecord> GetAllByStatusUpdateDateByStatus(string resourceGroupId, DateTime fromDate, string status = "*");
        List<BackupRecord> GetAllExpired();
        List<BackupRecord> GetAllByDatabaseIdByStatus(string resourceGroupId, string id, string status = "*");
        List<BackupRecord> GetAllReadyAndPendingDelivery();
        bool UpdateDeliveryRunned(string backupRecordId, bool hasRun, string executedDeliveryRunStatus);
    }
}
