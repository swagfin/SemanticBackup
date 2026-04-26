using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace SemanticBackup.IntegrationTests.Helpers
{
    public static class MySqlTestHelper
    {
        public static async Task ExecuteNonQueryAsync(string connectionString, string sql)
        {
            await using MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await using MySqlCommand command = new MySqlCommand(sql, connection);
            _ = await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();
        }

        public static async Task CreateDatabaseAsync(string rootConnectionString, string databaseName)
        {
            string safeDatabaseName = ToSafeIdentifier(databaseName);
            string sql = $"CREATE DATABASE IF NOT EXISTS `{safeDatabaseName}`;";
            await ExecuteNonQueryAsync(rootConnectionString, sql);
        }

        public static async Task DropDatabaseIfExistsAsync(string rootConnectionString, string databaseName)
        {
            string safeDatabaseName = ToSafeIdentifier(databaseName);
            string sql = $"DROP DATABASE IF EXISTS `{safeDatabaseName}`;";
            await ExecuteNonQueryAsync(rootConnectionString, sql);
        }

        private static string ToSafeIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Database name can not be empty.", nameof(input));
            if (!Regex.IsMatch(input, "^[A-Za-z0-9_]+$"))
                throw new ArgumentException($"Invalid identifier: {input}", nameof(input));
            return input;
        }
    }
}
