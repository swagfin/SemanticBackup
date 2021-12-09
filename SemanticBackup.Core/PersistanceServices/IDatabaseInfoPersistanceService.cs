using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IDatabaseInfoPersistanceService
    {
        List<BackupDatabaseInfo> GetAll(string directory);
        BackupDatabaseInfo GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(BackupDatabaseInfo record);
        bool Update(BackupDatabaseInfo record);
    }
}
