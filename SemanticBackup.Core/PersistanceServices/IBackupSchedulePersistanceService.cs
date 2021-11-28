using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupSchedulePersistanceService
    {
        List<BackupSchedule> GetAll();
        BackupSchedule GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupSchedule record);
        bool Update(BackupSchedule record);
        List<BackupSchedule> GetAllDueByDate(DateTime dateTime);
    }
}
