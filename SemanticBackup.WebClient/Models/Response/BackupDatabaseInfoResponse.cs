namespace SemanticBackup.WebClient.Models.Response
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
        public int BackupExpiryAgeInDays { get; set; }
        public string ColorCode
        {
            get
            {
                if (DatabaseType.Contains("SQLSERVER"))
                    return "orange";
                else if (DatabaseType.Contains("MYSQL"))
                    return "teal";
                else if (DatabaseType.Contains("MARIADB"))
                    return "blue";
                else
                    return "gray";
            }
        }
    }
}
