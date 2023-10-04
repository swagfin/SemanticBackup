using MySql.Data.MySqlClient;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Logic
{
    public class BackupProviderForMySQLServer : IBackupProviderForMySQLServer
    {
        public async Task<bool> BackupDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord)
        {
            if (string.IsNullOrWhiteSpace(backupDatabaseInfo.DatabaseConnectionString))
                throw new Exception($"Database Connection string for Database Type: {backupDatabaseInfo.DatabaseType} is not Valid or is not Supported");
            using (MySqlConnection conn = new MySqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        await conn.OpenAsync();
                        cmd.Connection = conn;
                        mb.ExportToFile(backupRecord.Path.Trim());
                        await conn.CloseAsync();
                    }
                }
                return true;
            }
        }

        public async Task<List<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo)
        {
            List<string> availableDbs = new List<string>();
            string[] exclude = new string[] { "information_schema", "mysql", "performance_schema" };
            using (MySqlConnection conn = new MySqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SHOW DATABASES;"))
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                string dbName = reader?.GetString(0);
                                if (!exclude.Contains(dbName))
                                    availableDbs.Add(dbName);
                            }
                        }
                        await reader.CloseAsync();
                    }
                    await conn.CloseAsync();
                }
            }
            return availableDbs;
        }
    }
}
