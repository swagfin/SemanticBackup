using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupRecordPersistanceService
    {
        List<BackupRecord> GetAll(string resourcegroup);
        BackupRecord GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupRecord record);
        bool Update(BackupRecord record);
        bool UpdateStatusFeed(string id, string status, DateTime updateDate, string message = null, long executionInMilliseconds = 0, string newFilePath = null);
        List<BackupRecord> GetAllByStatus(string status);
        List<BackupRecord> GetAllByDatabaseId(string id);
        List<BackupRecord> GetAllByRegisteredDateByStatus(string resourcegroup, DateTime fromDate, string status = "*");
        List<BackupRecord> GetAllByStatusUpdateDateByStatus(string resourcegroup, DateTime fromDate, string status = "*");
        List<BackupRecord> GetAllExpired(DateTime currentDate);
        List<BackupRecord> GetAllByDatabaseIdByStatus(string resourcegroup, string id, string status = "*");
    }
}
