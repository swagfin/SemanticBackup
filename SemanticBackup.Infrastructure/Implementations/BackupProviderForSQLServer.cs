using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class BackupProviderForSQLServer : IBackupProviderForSQLServer
    {
        public async Task<bool> BackupDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord)
        {
            string backupCommandTemplate = @"
BACKUP DATABASE [{0}] 
TO  DISK = N'{1}' 
WITH NOFORMAT, NOINIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10;
";
            string connectionString = resourceGroup.GetDbConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"Invalid connection string provided for Database Type: {resourceGroup.DbType} is not Valid or is not Supported");
            using DbConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            DbCommand command = connection.CreateCommand();
            command.CommandTimeout = 0; // Backups can take a long time for big databases
            command.CommandText = string.Format(backupCommandTemplate, databaseName, backupRecord.Path.Trim());
            //Execute
            int queryRows = await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();
            return true;
        }

        public async Task<bool> RestoreDatabaseAsync(string databaseName, ResourceGroup resourceGroup, BackupRecord backupRecord)
        {
            string restoreCommandTemplate = @"
USE [master]
RESTORE DATABASE [{0}] 
FROM  DISK = N'{1}' 
WITH NOUNLOAD, REPLACE, STATS = 5;
";
            string connectionString = resourceGroup.GetDbConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"Invalid connection string provided for Database Type: {resourceGroup.DbType} is not Valid or is not Supported");
            if (string.IsNullOrEmpty(backupRecord.Path))
                throw new Exception("Source Location can't be NULL");
            using DbConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            DbCommand command = connection.CreateCommand();
            command.CommandTimeout = 0; // Backups can take a long time for big databases
            command.CommandText = string.Format(restoreCommandTemplate, databaseName, backupRecord.Path);
            //Execute
            int queryRows = await command.ExecuteNonQueryAsync();
            connection.Close();
            return true;
        }

        public async Task<List<string>> GetAvailableDatabaseCollectionAsync(ResourceGroup resourceGroup)
        {
            List<string> availableDbs = new List<string>();
            string[] exclude = ["master", "model", "msdb", "tempdb"];
            string connectionString = resourceGroup.GetDbConnectionString();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using SqlCommand cmd = new SqlCommand("SELECT name FROM master.dbo.sysdatabases");
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
            return availableDbs;
        }
        public async Task<(bool success, string err)> TryTestDbConnectivityAsync(ResourceGroup resourceGroup)
        {
            try
            {
                string connectionString = resourceGroup.GetDbConnectionString();
                using SqlConnection conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                await conn.CloseAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
