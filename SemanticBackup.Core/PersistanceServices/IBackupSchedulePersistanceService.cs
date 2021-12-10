﻿using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IBackupSchedulePersistanceService
    {
        List<BackupSchedule> GetAll(string resourcegroup);
        BackupSchedule GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupSchedule record);
        bool Update(BackupSchedule record);
        List<BackupSchedule> GetAllDueByDate();
        List<BackupSchedule> GetAllByDatabaseId(string id);
    }
}
