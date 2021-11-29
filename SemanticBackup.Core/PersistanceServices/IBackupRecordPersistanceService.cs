using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupRecordPersistanceService
    {
        List<BackupRecord> GetAll();
        BackupRecord GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupRecord record);
        bool Update(BackupRecord record);
        bool UpdateStatusFeed(string id, string status, DateTime updateDate, string message = null, long executionInMilliseconds = 0);
        List<BackupRecord> GetAllByStatus(string status);
        List<BackupRecord> GetAllByDatabaseId(string id);
    }
}
