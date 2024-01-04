using MySql.Data.MySqlClient;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class BackupProviderForMySQLServer : IBackupProviderForMySQLServer
    {
        public async Task<bool> BackupDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord)
        {
            string connectionString = resourceGroup.GetDbConnectionString(databaseName);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"Invalid connection string provided for Database Type: {resourceGroup.DbType} is not Valid or is not Supported");
            using (MySqlConnection conn = new MySqlConnection(connectionString))
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

        public async Task<List<string>> GetAvailableDatabaseCollectionAsync(ResourceGroup resourceGroup)
        {
            List<string> availableDbs = new List<string>();
            string[] exclude = new string[] { "information_schema", "mysql", "performance_schema" };
            string connectionString = resourceGroup.GetDbConnectionString();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
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

        public async Task<(bool success, string err)> TryTestDbConnectivityAsync(ResourceGroup resourceGroup)
        {
            try
            {
                string connectionString = resourceGroup.GetDbConnectionString();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    await conn.CloseAsync();
                    return (true, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
