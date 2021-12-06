using MySql.Data.MySqlClient;
using SemanticBackup.Core.Models;
using System;

namespace SemanticBackup.Core.ProviderServices.Implementations
{
    public class MySQLServerBackupProviderService : IMySQLServerBackupProviderService
    {
        public bool BackupDatabase(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord)
        {
            if (string.IsNullOrWhiteSpace(backupDatabaseInfo.DatabaseConnectionString))
                throw new Exception($"Database Connection string for Database Type: {backupDatabaseInfo.DatabaseType} is not Valid or is not Supported");
            using (MySqlConnection conn = new MySqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        conn.Open();
                        cmd.Connection = conn;
                        mb.ExportToFile(backupRecord.Path.Trim());
                        conn.Close();
                    }
                }
                return true;
            }
        }
    }
}
