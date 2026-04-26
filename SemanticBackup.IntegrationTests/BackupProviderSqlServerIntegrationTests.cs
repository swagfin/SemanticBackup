using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.Implementations;
using SemanticBackup.IntegrationTests.Helpers;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.IntegrationTests
{
    [Collection(SemanticBackupIntegrationCollection.CollectionName)]
    public class BackupProviderSqlServerIntegrationTests
    {
        private readonly SqlServerContainerFixture _sqlFixture;

        public BackupProviderSqlServerIntegrationTests(SqlServerContainerFixture sqlFixture)
        {
            _sqlFixture = sqlFixture;
        }

        [Fact]
        public async Task BackupProviderForSqlServer_ShouldCreateValidBackupFile()
        {
            if (!_sqlFixture.IsDockerAvailable)
                return;

            string databaseName = $"sb_it_{Guid.NewGuid():N}".Substring(0, 14);
            string backupPath = $"/var/opt/mssql/data/{databaseName}.bak";
            string dbConnectionString = _sqlFixture.BuildConnectionString(databaseName);

            await SqlServerTestHelper.DropDatabaseIfExistsAsync(_sqlFixture.MasterConnectionString, databaseName);
            await SqlServerTestHelper.CreateDatabaseAsync(_sqlFixture.MasterConnectionString, databaseName);

            try
            {
                await SqlServerTestHelper.ExecuteNonQueryAsync(dbConnectionString, "CREATE TABLE dbo.Sample (Id INT PRIMARY KEY, Name NVARCHAR(50));");
                await SqlServerTestHelper.ExecuteNonQueryAsync(dbConnectionString, "INSERT INTO dbo.Sample (Id, Name) VALUES (1, 'semantic');");

                BackupProviderForSQLServer provider = new BackupProviderForSQLServer();
                ResourceGroup resourceGroup = new ResourceGroup
                {
                    Id = "it-resource",
                    Name = "Integration Resource",
                    DbType = DbTypes.SQLSERVER2019.ToString(),
                    DbServer = "container",
                    DbUsername = "sa",
                    ConnectionString = _sqlFixture.MasterConnectionString
                };
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 1001,
                    BackupDatabaseInfoId = "db-1001",
                    Path = backupPath
                };

                bool backupSuccess = await provider.BackupDatabaseAsync(databaseName, resourceGroup, backupRecord);
                Assert.True(backupSuccess);

                object? headerResult = await SqlServerTestHelper.ExecuteScalarAsync(_sqlFixture.MasterConnectionString, $"RESTORE HEADERONLY FROM DISK = N'{backupPath}';");
                Assert.NotNull(headerResult);
            }
            finally
            {
                await SqlServerTestHelper.DropDatabaseIfExistsAsync(_sqlFixture.MasterConnectionString, databaseName);
            }
        }
    }
}
