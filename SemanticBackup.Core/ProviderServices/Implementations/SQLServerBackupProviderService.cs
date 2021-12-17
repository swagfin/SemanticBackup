using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Core.ProviderServices.Implementations
{
    public class SQLServerBackupProviderService : ISQLServerBackupProviderService
    {
        public const string BackupCommandTemplate = @"
BACKUP DATABASE [{0}] 
TO  DISK = N'{1}' 
WITH NOFORMAT, NOINIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10;
";
        public async Task<bool> BackupDatabaseAsync(BackupDatabaseInfo backupDatabaseInfo, BackupRecord backupRecord)
        {
            if (string.IsNullOrWhiteSpace(backupDatabaseInfo.DatabaseConnectionString))
                throw new Exception($"Database Connection string for Database Type: {backupDatabaseInfo.DatabaseType} is not Valid or is not Supported");
            using (DbConnection connection = new SqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                await connection.OpenAsync();
                DbCommand command = connection.CreateCommand();
                command.CommandTimeout = 0; // Backups can take a long time for big databases
                command.CommandText = string.Format(BackupCommandTemplate, backupDatabaseInfo.DatabaseName, backupRecord.Path.Trim());
                //Execute
                int queryRows = await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
                return true;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableDatabaseCollectionAsync(BackupDatabaseInfo backupDatabaseInfo)
        {
            List<string> availableDbs = new List<string>();
            string[] exclude = new string[] { "master", "model", "msdb", "tempdb" };
            using (SqlConnection conn = new SqlConnection(backupDatabaseInfo.DatabaseConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT name FROM master.dbo.sysdatabases"))
                {
                    await conn.OpenAsync();
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
                        await reader.CloseAsync();
                    }
                    await conn.CloseAsync();
                }
            }
            return availableDbs;
        }
    }
}
