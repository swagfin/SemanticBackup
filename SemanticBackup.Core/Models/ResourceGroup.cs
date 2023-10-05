using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class ResourceGroup
    {
        [Key, Required]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string Key { get { return Name.FormatToUrlStyle(); } }
        [Required]
        public string Name { get; set; }
        //Shared Db Connection Configs
        [Required]
        public string DbServer { get; set; } = "127.0.0.1";
        [Required]
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public int DbPort { get; set; } = 0;
        [Required]
        public string DbType { get; set; } = DbTypes.SQLSERVER2019.ToString();
        //Other Configs
        public int MaximumRunningBots { get; set; } = 1;
        public bool CompressBackupFiles { get; set; } = true;
        public int BackupExpiryAgeInDays { get; set; } = 7;
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
        public long LastAccess { get; set; } = 0;
    }

    public enum DbTypes
    {
        SQLSERVER2019, SQLSERVER2014, SQLSERVER2012, MARIADBDATABASE, MYSQLDATABASE
    }
}
