namespace SemanticBackup.API.Models.Requests
{
    public class DatabaseCollectionRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
    }
}
