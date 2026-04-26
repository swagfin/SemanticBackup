namespace SemanticBackup.IntegrationTests.Helpers
{
    [CollectionDefinition(CollectionName, DisableParallelization = true)]
    public class SemanticBackupIntegrationCollection : ICollectionFixture<SqlServerContainerFixture>, ICollectionFixture<MinioContainerFixture>, ICollectionFixture<FtpContainerFixture>
    {
        public const string CollectionName = "semanticbackup-integration-tests";
    }
}
