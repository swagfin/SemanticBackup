using System.Collections.Generic;

namespace SemanticBackup.Core.Models
{
    public class AdminUserConfiguration
    {
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public string Timezone { get; set; } = "Africa/Nairobi";
        public string TimezoneOffset { get; set; } = "+03:00";
    }

    public class NotificationConfigs
    {
        public bool NotifyOnBackupFailure { get; set; } = false;
        public bool NotifyOnUploadFailure { get; set; } = false;
        public List<string> NotifyEmailDestinations { get; set; } = new List<string>();
    }

    public class BackupResourceConfiguration
    {
        public string Name { get; set; }
        public string Type { get; set; } = DbTypes.SQLSERVER2019.ToString();
        public string ConnectionString { get; set; }
        public NotificationConfigs NotificationConfigs { get; set; } = new NotificationConfigs();
        public List<string> UploadTo { get; set; } = new List<string>();
        public List<BackupResourceDatabaseConfiguration> Databases { get; set; } = new List<BackupResourceDatabaseConfiguration>();
    }

    public class BackupResourceDatabaseConfiguration
    {
        public string Name { get; set; }
        public int BackupEveryHrs { get; set; } = 24;
        public bool FullBackup { get; set; } = true;
    }

    public class UploadConfigurations
    {
        public DownloadLinkDeliveryConfig DownloadLink { get; set; } = new DownloadLinkDeliveryConfig();
        public FtpDeliveryConfig Ftp { get; set; } = new FtpDeliveryConfig();
        public SmtpDeliveryConfig Smtp { get; set; } = new SmtpDeliveryConfig();
        public ObjectStorageDeliveryConfig ObjectStorage { get; set; } = new ObjectStorageDeliveryConfig();
        public AzureBlobStorageDeliveryConfig AzureBlob { get; set; } = new AzureBlobStorageDeliveryConfig();
        public DropboxDeliveryConfig Dropbox { get; set; } = new DropboxDeliveryConfig();
    }
}
