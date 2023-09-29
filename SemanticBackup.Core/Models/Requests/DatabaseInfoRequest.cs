using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models.Requests
{
    public class DatabaseInfoRequest
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
        public string DatabaseType { get; set; } = BackupDatabaseInfoDbType.SQLSERVER2019.ToString();
        public string Description { get; set; }
        public bool AutoCreateSchedule { get; set; } = true;
    }
}
