﻿using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Interfaces
{
    public interface IBackupProviderForMySQLServer
    {
        Task<bool> BackupDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord);
        Task<IEnumerable<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo);
    }
}