using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Minio;
using Minio.DataModel.Args;
using System.Net.Sockets;

namespace SemanticBackup.IntegrationTests.Helpers
{
    public class MinioContainerFixture : IAsyncLifetime
    {
        private IContainer? _container;
        public bool IsDockerAvailable { get; private set; } = true;
        public string DockerUnavailableReason { get; private set; } = string.Empty;
        public string AccessKey { get; } = "semanticuser";
        public string SecretKey { get; } = "semanticpassword";
        public string Bucket { get; } = "backups";
        public string Server { get; private set; } = "127.0.0.1";
        public int Port { get; private set; }

        public async Task InitializeAsync()
        {
            try
            {
                _container = new ContainerBuilder()
                    .WithImage("minio/minio:latest")
                    .WithName("semanticbackup-minio")
                    .WithEnvironment("MINIO_ROOT_USER", AccessKey)
                    .WithEnvironment("MINIO_ROOT_PASSWORD", SecretKey)
                    .WithCommand("server", "/data", "--address", ":9000")
                    .WithPortBinding(9000, true)
                    .WithReuse(true)
                    .Build();

                await _container.StartAsync();
                Port = _container.GetMappedPublicPort(9000);
                await WaitForPortReadyAsync(Server, Port, CancellationToken.None);
                await EnsureBucketExistsAsync(CancellationToken.None);
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

        private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
        {
            using IMinioClient minioClient = new MinioClient().WithEndpoint(Server, Port).WithCredentials(AccessKey, SecretKey).WithSSL(false).Build();
            BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(Bucket);
            bool bucketExists = await minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            if (!bucketExists)
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket), cancellationToken);
        }

        private static async Task WaitForPortReadyAsync(string host, int port, CancellationToken cancellationToken)
        {
            const int maxAttempts = 20;
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
    }
}
