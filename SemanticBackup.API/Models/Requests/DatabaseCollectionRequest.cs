namespace SemanticBackup.API.Models.Requests
{
    public class DatabaseCollectionRequest
    {
        public string Type { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Port { get; set; }
    }
}
