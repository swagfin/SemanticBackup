﻿using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.Services
{
    public interface IBackupSchedulePersistanceService
    {
        List<BackupSchedule> GetAll();
        BackupSchedule GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupSchedule record);
        bool Update(BackupSchedule record);
    }
}
