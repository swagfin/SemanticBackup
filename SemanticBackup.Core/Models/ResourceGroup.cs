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
        //delivery Configs
        public BackupDeliveryConfig BackupDeliveryConfig { get; set; } = new BackupDeliveryConfig();
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
    }

    public class BackupDeliveryConfig
    {
        public DownloadLinkDeliveryConfig DownloadLink { get; set; } = new DownloadLinkDeliveryConfig();
        public FtpDeliveryConfig Ftp { get; set; } = new FtpDeliveryConfig();
        public SmtpDeliveryConfig Smtp { get; set; } = new SmtpDeliveryConfig();
        public DropboxDeliveryConfig Dropbox { get; set; } = new DropboxDeliveryConfig();
        public AzureBlobStorageDeliveryConfig AzureBlobStorage { get; set; } = new AzureBlobStorageDeliveryConfig();
        public ObjectStorageDeliveryConfig ObjectStorage { get; set; } = new ObjectStorageDeliveryConfig();
    }

    public class DownloadLinkDeliveryConfig
    {
        public bool IsEnabled { get; set; } = true;
        public string DownloadLinkType { get; set; }
    }

    public class FtpDeliveryConfig
    {
        public bool IsEnabled { get; set; } = false;
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Directory { get; set; } = "/";
    }

    public class SmtpDeliveryConfig
    {
        public bool IsEnabled { get; set; } = false;
        public bool SMTPEnableSSL { get; set; } = true;
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; } = 587;
        public string SMTPEmailAddress { get; set; }
        public string SMTPEmailCredentials { get; set; }
        public string SMTPDefaultSMTPFromName { get; set; }
        public string SMTPDestinations { get; set; }
    }

    public class DropboxDeliveryConfig
    {
        public bool IsEnabled { get; set; } = false;
        public string AccessToken { get; set; }
        public string Directory { get; set; } = "/";
    }
    public class AzureBlobStorageDeliveryConfig
    {
        public bool IsEnabled { get; set; } = false;
        public string ConnectionString { get; set; }
        public string BlobContainer { get; set; }
    }

    public class ObjectStorageDeliveryConfig
    {
        public bool IsEnabled { get; set; } = false;
        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 9000;
        public string Bucket { get; set; } = "public";
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;
    }

    //enums
    public enum DbTypes
    {
        SQLSERVER2019, SQLSERVER2014, SQLSERVER2012, MARIADBDATABASE, MYSQLDATABASE
    }
    public enum BackupDeliveryConfigTypes
    {
        DownloadLink, Ftp, Smtp, Dropbox, AzureBlobStorage, ObjectStorage
    }
}
