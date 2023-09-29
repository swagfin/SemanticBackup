using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupDatabaseInfo
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        public string Name { get { return $"{DatabaseName} on {Server}"; } }
        public string Description { get; set; }
        [Required]
        public string Server { get; set; } = "127.0.0.1";
        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 0;
        [Required, MinLength(3)]
        public string DatabaseName { get; set; }
        [Required]
        public string DatabaseType { get; set; } = BackupDatabaseInfoDbType.SQLSERVER2019.ToString();
        public string DatabaseConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(DatabaseType) && DatabaseType.Contains("SQLSERVER"))
                    return $"Data Source={Server},{Port};Initial Catalog={DatabaseName};Persist Security Info=True;User ID={Username};Password={Password};";
                else if (DatabaseType.Contains("MYSQL") || DatabaseType.Contains("MARIADB"))
                    return $"server={Server};uid={Username};pwd={Password};database={DatabaseName};port={Port};CharSet=utf8;Connection Timeout=300";
                else
                    return null;
            }
        }
        public DateTime DateRegisteredUTC { get; set; } = DateTime.UtcNow;

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
    public enum BackupDatabaseInfoDbType
    {
        SQLSERVER2019, SQLSERVER2014, SQLSERVER2012, MARIADBDATABASE, MYSQLDATABASE
    }
}
