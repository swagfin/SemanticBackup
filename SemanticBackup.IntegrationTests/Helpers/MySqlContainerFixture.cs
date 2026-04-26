using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MySql.Data.MySqlClient;
using System.Net.Sockets;

namespace SemanticBackup.IntegrationTests.Helpers
{
    public class MySqlContainerFixture : IAsyncLifetime
    {
        private const string RootPassword = "semantic-root-password";
        private IContainer? _container;
        public bool IsDockerAvailable { get; private set; } = true;
        public string DockerUnavailableReason { get; private set; } = string.Empty;
        public string Server { get; private set; } = "127.0.0.1";
        public int Port { get; private set; }
        public string RootUsername { get; } = "root";

        public string MasterConnectionString
        {
            get
            {
                if (_container == null)
                    throw new InvalidOperationException("MariaDB test container is not initialized.");
                return $"server={Server};uid={RootUsername};pwd={RootPassword};port={Port};Connection Timeout=30;Allow Zero Datetime=True;Convert Zero Datetime=True;";
            }
        }

        public string BuildConnectionString(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                return MasterConnectionString;
            return $"{MasterConnectionString}database={databaseName};";
        }

        public async Task InitializeAsync()
        {
            try
            {
                _container = new ContainerBuilder()
                    .WithImage("mariadb:11.4")
                    .WithName("semanticbackup-mariadb")
                    .WithEnvironment("MARIADB_ROOT_PASSWORD", RootPassword)
                    .WithPortBinding(3306, true)
                    .WithReuse(true)
                    .Build();

                await _container.StartAsync();
                Port = _container.GetMappedPublicPort(3306);
                await WaitForPortReadyAsync(Server, Port, CancellationToken.None);
                await WaitForMySqlReadyAsync(MasterConnectionString, CancellationToken.None);
            }
            catch (Exception ex)
            {
                IsDockerAvailable = false;
                DockerUnavailableReason = ex.Message;
            }
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }

        private static async Task WaitForPortReadyAsync(string host, int port, CancellationToken cancellationToken)
        {
            const int maxAttempts = 30;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using TcpClient tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync(host, port, cancellationToken);
                    return;
                }
                catch when (attempt < maxAttempts)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
            throw new Exception($"Port check failed for {host}:{port}");
        }

        private static async Task WaitForMySqlReadyAsync(string connectionString, CancellationToken cancellationToken)
        {
            const int maxAttempts = 30;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using MySqlConnection connection = new MySqlConnection(connectionString);
                    await connection.OpenAsync(cancellationToken);
                    await using MySqlCommand command = new MySqlCommand("SELECT 1;", connection);
                    _ = await command.ExecuteScalarAsync(cancellationToken);
                    return;
                }
                catch when (attempt < maxAttempts)
                {
                    await Task.Delay(750, cancellationToken);
                }
            }
            throw new Exception("MariaDB startup validation failed.");
        }
    }
}
