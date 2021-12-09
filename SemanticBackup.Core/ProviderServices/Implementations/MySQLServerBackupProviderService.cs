using MySql.Data.MySqlClient;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo)
        {
            List<string> availableDbs = new List<string>();
            string[] exclude = new string[] { "information_schema", "mysql", "performance_schema" };
            using (MySqlConnection conn = new MySqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SHOW DATABASES;"))
                {
                    conn.Open();
                    cmd.Connection = conn;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string dbName = reader?.GetString(0);
                                if (!exclude.Contains(dbName))
                                    availableDbs.Add(dbName);
                            }
                        }
                        reader.Close();
                    }
                    conn.Close();
                }
            }
            return availableDbs;
        }
    }
}
