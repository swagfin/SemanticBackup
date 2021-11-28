using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.Services
{
    public interface IBackupRecordPersistanceService
    {
        List<BackupRecord> GetAll();
        BackupRecord GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupRecord record);
        bool Update(BackupRecord record);
    }
}
