using SemanticBackup.Core.Models;
using SemanticBackup.Infrastructure.Implementations;
using SemanticBackup.IntegrationTests.Helpers;

namespace SemanticBackup.IntegrationTests
{
    [Collection(SemanticBackupIntegrationCollection.CollectionName)]
    public class BackupProviderMySqlIntegrationTests
    {
        private readonly MySqlContainerFixture _mySqlFixture;

        public BackupProviderMySqlIntegrationTests(MySqlContainerFixture mySqlFixture)
        {
            _mySqlFixture = mySqlFixture;
        }

        [Fact]
        public async Task BackupProviderForMySql_ShouldCreateBackupFile_WhenConnectionStringHasNoDatabase()
        {
            if (!_mySqlFixture.IsDockerAvailable)
                return;

            string databaseName = $"sbmariadb_{Guid.NewGuid():N}".Substring(0, 18);
            string backupPath = Path.Combine(Path.GetTempPath(), $"{databaseName}.sql");
            string dbConnectionString = _mySqlFixture.BuildConnectionString(databaseName);

            await MySqlTestHelper.DropDatabaseIfExistsAsync(_mySqlFixture.MasterConnectionString, databaseName);
            await MySqlTestHelper.CreateDatabaseAsync(_mySqlFixture.MasterConnectionString, databaseName);

            try
            {
                await MySqlTestHelper.ExecuteNonQueryAsync(dbConnectionString, "CREATE TABLE sample_data (id INT PRIMARY KEY, name VARCHAR(40));");
                await MySqlTestHelper.ExecuteNonQueryAsync(dbConnectionString, "INSERT INTO sample_data (id, name) VALUES (1, 'semantic');");

                BackupProviderForMySQLServer provider = new BackupProviderForMySQLServer();
                ResourceGroup resourceGroup = new ResourceGroup
                {
                    Id = "it-resource-mariadb",
                    Name = "MariaDB Integration Resource",
                    DbType = DbTypes.MARIADBDATABASE.ToString(),
                    ConnectionString = _mySqlFixture.MasterConnectionString,
                    DbServer = "container",
                    DbUsername = "root"
                };
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 6001,
                    BackupDatabaseInfoId = "db-mariadb-6001",
                    Path = backupPath
                };

                bool backupSuccess = await provider.BackupDatabaseAsync(databaseName, resourceGroup, backupRecord);
                Assert.True(backupSuccess);
                Assert.True(File.Exists(backupPath));

                string dumpContents = await File.ReadAllTextAsync(backupPath);
                Assert.Contains("sample_data", dumpContents, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await MySqlTestHelper.DropDatabaseIfExistsAsync(_mySqlFixture.MasterConnectionString, databaseName);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }

        [Fact]
        public async Task BackupProviderForMySql_ShouldOverrideDatabaseInProvidedConnectionString()
        {
            if (!_mySqlFixture.IsDockerAvailable)
                return;

            string targetDatabaseName = $"sbmariareg_{Guid.NewGuid():N}".Substring(0, 18);
            string backupPath = Path.Combine(Path.GetTempPath(), $"{targetDatabaseName}.sql");
            string dbConnectionString = _mySqlFixture.BuildConnectionString(targetDatabaseName);
            string markerTableName = $"marker_{Guid.NewGuid():N}".Substring(0, 18);

            await MySqlTestHelper.DropDatabaseIfExistsAsync(_mySqlFixture.MasterConnectionString, targetDatabaseName);
            await MySqlTestHelper.CreateDatabaseAsync(_mySqlFixture.MasterConnectionString, targetDatabaseName);

            try
            {
                await MySqlTestHelper.ExecuteNonQueryAsync(dbConnectionString, $"CREATE TABLE {markerTableName} (id INT PRIMARY KEY, name VARCHAR(40));");
                await MySqlTestHelper.ExecuteNonQueryAsync(dbConnectionString, $"INSERT INTO {markerTableName} (id, name) VALUES (1, 'regression');");

                BackupProviderForMySQLServer provider = new BackupProviderForMySQLServer();
                ResourceGroup resourceGroup = new ResourceGroup
                {
                    Id = "it-resource-mariadb-regression",
                    Name = "MariaDB Regression Resource",
                    DbType = DbTypes.MARIADBDATABASE.ToString(),
                    ConnectionString = _mySqlFixture.BuildConnectionString("mysql"),
                    DbServer = "container",
                    DbUsername = "root"
                };
                BackupRecord backupRecord = new BackupRecord
                {
                    Id = 6002,
                    BackupDatabaseInfoId = "db-mariadb-6002",
                    Path = backupPath
                };

                bool backupSuccess = await provider.BackupDatabaseAsync(targetDatabaseName, resourceGroup, backupRecord);
                Assert.True(backupSuccess);
                Assert.True(File.Exists(backupPath));

                string dumpContents = await File.ReadAllTextAsync(backupPath);
                Assert.Contains(markerTableName, dumpContents, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await MySqlTestHelper.DropDatabaseIfExistsAsync(_mySqlFixture.MasterConnectionString, targetDatabaseName);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }
    }
}
