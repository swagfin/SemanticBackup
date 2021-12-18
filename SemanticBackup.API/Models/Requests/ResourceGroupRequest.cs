using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
{
    public class ResourceGroupRequest
    {
        [Required]
        public string Name { get; set; }
        public string TimeZone { get; set; } = null;
        [Range(1, 50)]
        public int MaximumRunningBots { get; set; } = 1;
        public bool CompressBackupFiles { get; set; } = true;
        [Range(1, 366)]
        public int BackupExpiryAgeInDays { get; set; } = 7;
        public RSDownloadLinkSetting RSDownloadLinkSetting { get; set; } = null;
        public RSFTPSetting RSFTPSetting { get; set; } = null;
        public RSEmailSMTPSetting RSEmailSMTPSetting { get; set; } = null;
        public RSDropBoxSetting RSDropBoxSetting { get; set; } = null;
        public RSAzureBlobStorageSetting RSAzureBlobStorageSetting { get; set; } = null;
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
    }
}
