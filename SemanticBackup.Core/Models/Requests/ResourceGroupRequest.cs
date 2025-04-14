using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models.Requests
{
    public class ResourceGroupRequest
    {
        [Required]
        public string Name { get; set; }
        [Range(1, 50)]
        public int MaximumRunningBots { get; set; } = 1;

        [Required]
        public string DbServer { get; set; } = "127.0.0.1";
        [Required]
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public int DbPort { get; set; } = 0;
        [Required]
        public string DbType { get; set; } = DbTypes.SQLSERVER2019.ToString();

        public bool CompressBackupFiles { get; set; } = true;
        [Range(1, 366)]
        public int BackupExpiryAgeInDays { get; set; } = 7;
        public RSDownloadLinkSetting RSDownloadLinkSetting { get; set; } = null;
        public RSFTPSetting RSFTPSetting { get; set; } = null;
        public RSEmailSMTPSetting RSEmailSMTPSetting { get; set; } = null;
        public RSDropBoxSetting RSDropBoxSetting { get; set; } = null;
        public RSAzureBlobStorageSetting RSAzureBlobStorageSetting { get; set; } = null;
        public RSObjectStorageSetting RSObjectStorageSetting { get; set; } = null;
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
    }
}
