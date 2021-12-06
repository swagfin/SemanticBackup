using SemanticBackup.Core.Models;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SemanticBackup.Core.ProviderServices.Implementations
{
    public class SQLServerBackupProviderService : ISQLServerBackupProviderService
    {
        public const string BackupCommandTemplate = @"
BACKUP DATABASE [{0}] 
TO  DISK = N'{1}' 
WITH NOFORMAT, NOINIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10;
";
        public bool BackupDatabase(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord)
        {
            if (string.IsNullOrWhiteSpace(backupDatabaseInfo.DatabaseConnectionString))
                throw new Exception($"Database Connection string for Database Type: {backupDatabaseInfo.DatabaseType} is not Valid or is not Supported");
            using (DbConnection connection = new SqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                connection.Open();
                DbCommand command = connection.CreateCommand();
                command.CommandTimeout = 0; // Backups can take a long time for big databases
                command.CommandText = string.Format(BackupCommandTemplate, backupDatabaseInfo.DatabaseName, backupRecord.Path.Trim());
                //Execute
                int queryRows = command.ExecuteNonQuery();
                connection.Close();
                return true;
            }
        }
    }
}
