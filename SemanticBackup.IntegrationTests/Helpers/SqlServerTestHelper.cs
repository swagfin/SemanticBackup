using System.Data.SqlClient;

namespace SemanticBackup.IntegrationTests.Helpers
{
    internal static class SqlServerTestHelper
    {
        public static async Task CreateDatabaseAsync(string masterConnectionString, string databaseName, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqlCommand command = connection.CreateCommand();
            string escapedDb = EscapeSqlIdentifier(databaseName);
            command.CommandText = $@"
IF DB_ID(@dbName) IS NULL
BEGIN
    CREATE DATABASE [{escapedDb}];
END;";
            command.Parameters.AddWithValue("@dbName", databaseName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public static async Task DropDatabaseIfExistsAsync(string masterConnectionString, string databaseName, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqlCommand command = connection.CreateCommand();
            string escapedDb = EscapeSqlIdentifier(databaseName);
            command.CommandText = $@"
IF DB_ID(@dbName) IS NOT NULL
BEGIN
    ALTER DATABASE [{escapedDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{escapedDb}];
END;";
            command.Parameters.AddWithValue("@dbName", databaseName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public static async Task ExecuteNonQueryAsync(string connectionString, string commandText, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public static async Task<object?> ExecuteScalarAsync(string connectionString, string commandText, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            return await command.ExecuteScalarAsync(cancellationToken);
        }

        private static string EscapeSqlIdentifier(string name) => (name ?? string.Empty).Replace("]", "]]", StringComparison.Ordinal);
    }
}
