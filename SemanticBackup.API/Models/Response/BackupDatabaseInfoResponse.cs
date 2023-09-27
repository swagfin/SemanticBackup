namespace SemanticBackup.Core.Models.Response
{
    public class BackupDatabaseInfoResponse
    {
        public string Id { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 0;
        public string DatabaseName { get; set; }
        public string DatabaseType { get; set; }
        public string Description { get; set; }
    }
}
