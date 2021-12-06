using SemanticBackup.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
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
        public string DatabaseType { get; set; } = BackupDatabaseInfoDbType.SQLSERVER2019.ToString();
        public string Description { get; set; }
        public int BackupExpiryAgeInDays { get; set; } = 14;
        public bool AutoCreateSchedule { get; set; } = true;
    }
}
