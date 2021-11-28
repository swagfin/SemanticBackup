using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.Services
{
    public interface IDatabaseInfoPersistanceService
    {
        List<BackupDatabaseInfo> GetAll();
        BackupDatabaseInfo GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupDatabaseInfo record);
        bool Update(BackupDatabaseInfo record);
    }
}
