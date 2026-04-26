using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace SemanticBackup.IntegrationTests.Helpers
{
    public class SqlServerContainerFixture : IAsyncLifetime
    {
        private const string ContainerPassword = "FaaFaaFaaaaaaPwd123!";
        private MsSqlContainer? _container;
        public bool IsDockerAvailable { get; private set; } = true;
        public string DockerUnavailableReason { get; private set; } = string.Empty;

        public SqlServerContainerFixture()
        {
            // Deferred to InitializeAsync to allow graceful skip when Docker is unavailable.
        }

        public string MasterConnectionString
        {
            get
            {
                if (_container == null)
                    throw new InvalidOperationException("SQL test container is not initialized.");
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
                {
                    InitialCatalog = "master",
                    Encrypt = false,
                    TrustServerCertificate = true
                };
                return builder.ConnectionString;
            }
        }

        public string BuildConnectionString(string databaseName)
        {
            if (_container == null)
                throw new InvalidOperationException("SQL test container is not initialized.");
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                    InitialCatalog = databaseName,
                    Encrypt = false,
                    TrustServerCertificate = true
            };
            return builder.ConnectionString;
        }

        public async Task InitializeAsync()
        {
            try
            {
                MsSqlBuilder builder = new MsSqlBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                    .WithPassword(ContainerPassword)
                    .WithReuse(true)
                    .WithName("semanticbackup-mssql-2019");
                _container = builder.Build();
                await _container.StartAsync();
                await WaitForSqlReadyAsync(MasterConnectionString, CancellationToken.None);
            }
            catch (DockerUnavailableException ex)
            {
                IsDockerAvailable = false;
                DockerUnavailableReason = ex.Message;
            }
        }

        public async Task DisposeAsync()
        {
            if (_container == null)
                return;
            await Task.CompletedTask;
        }

        private static async Task WaitForSqlReadyAsync(string connectionString, CancellationToken cancellationToken)
        {
            const int maxAttempts = 20;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using SqlConnection connection = new SqlConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);
                    await using SqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT 1;";
                    await command.ExecuteScalarAsync(cancellationToken);
                    return;
                }
                catch (SqlException) when (attempt < maxAttempts)
                {
                    await Task.Delay(750, cancellationToken);
                }
            }
        }
    }
}
