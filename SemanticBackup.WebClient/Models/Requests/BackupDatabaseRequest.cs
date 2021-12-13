using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.WebClient.Models.Requests
{
    public class BackupDatabaseRequest
    {
        [Required]
        public string Server { get; set; } = "127.0.0.1";
        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 0;
        [Required]
        public string DatabaseName { get; set; }
        [Required]
        public string DatabaseType { get; set; }
        public string Description { get; set; }
        public bool AutoCreateSchedule { get; set; } = true;
    }

    public enum BackupDatabaseInfoDbType
    {
        SQLSERVER2012, SQLSERVER2014, SQLSERVER2019, MARIADBDATABASE, MYSQLDATABASE
    }
}
