using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Sockets;

namespace SemanticBackup.IntegrationTests.Helpers
{
    public class FtpContainerFixture : IAsyncLifetime
    {
        private IContainer? _container;
        public bool IsDockerAvailable { get; private set; } = true;
        public string DockerUnavailableReason { get; private set; } = string.Empty;
        public string Username { get; } = "semanticuser";
        public string Password { get; } = "semanticpassword";
        public string Server { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            try
            {
                ContainerBuilder builder = new ContainerBuilder()
                    .WithImage("stilliard/pure-ftpd:hardened")
                    .WithName("semanticbackup-ftp")
                    .WithEnvironment("PUBLICHOST", "127.0.0.1")
                    .WithEnvironment("FTP_USER_NAME", Username)
                    .WithEnvironment("FTP_USER_PASS", Password)
                    .WithEnvironment("FTP_USER_HOME", "/home/semanticuser")
                    .WithEnvironment("ADDED_FLAGS", "-d -d -p 30100:30109")
                    .WithPortBinding(21, true)
                    .WithPortBinding(30100)
                    .WithPortBinding(30101)
                    .WithPortBinding(30102)
                    .WithPortBinding(30103)
                    .WithPortBinding(30104)
                    .WithPortBinding(30105)
                    .WithPortBinding(30106)
                    .WithPortBinding(30107)
                    .WithPortBinding(30108)
                    .WithPortBinding(30109)
                    .WithReuse(true);

                _container = builder.Build();
                await _container.StartAsync();
                int mappedControlPort = _container.GetMappedPublicPort(21);
                await WaitForPortReadyAsync("127.0.0.1", mappedControlPort);
                Server = $"ftp://127.0.0.1:{mappedControlPort}";
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

        private static async Task WaitForPortReadyAsync(string host, int port)
        {
            const int maxAttempts = 20;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using TcpClient tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync(host, port);
                    return;
                }
                catch when (attempt < maxAttempts)
                {
                    await Task.Delay(500);
                }
            }
            throw new Exception($"Port check failed for {host}:{port}");
        }
    }
}
