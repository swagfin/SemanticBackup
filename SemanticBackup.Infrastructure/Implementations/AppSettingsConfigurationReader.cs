using Microsoft.Extensions.Configuration;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.Infrastructure.Implementations
{
    internal class AppSettingsConfigurationReader
    {
        private readonly IConfiguration _configuration;

        public AppSettingsConfigurationReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<AdminUserConfiguration> GetAdminUsers()
        {
            List<AdminUserConfiguration> users = new List<AdminUserConfiguration>();
            _configuration.GetSection("AdminUsers").Bind(users);
            return users ?? new List<AdminUserConfiguration>();
        }

        public List<BackupResourceConfiguration> GetBackupConfigurations()
        {
            List<BackupResourceConfiguration> backupConfigurations = new List<BackupResourceConfiguration>();
            _configuration.GetSection("BackupConfigurations").Bind(backupConfigurations);
            return backupConfigurations ?? new List<BackupResourceConfiguration>();
        }

        public UploadConfigurations GetUploadConfigurations()
        {
            UploadConfigurations uploadConfigurations = new UploadConfigurations();
            _configuration.GetSection("UploadConfigurations").Bind(uploadConfigurations);
            return uploadConfigurations;
        }

        public NotificationConfigs GetGlobalNotificationConfigs()
        {
            NotificationConfigs notificationConfigs = new NotificationConfigs();
            _configuration.GetSection("NotificationConfigs").Bind(notificationConfigs);
            return notificationConfigs;
        }

        public SystemConfigOptions GetSystemConfigOptions()
        {
            SystemConfigOptions systemConfigOptions = new SystemConfigOptions();
            _configuration.GetSection("SystemConfigOptions").Bind(systemConfigOptions);
            return systemConfigOptions;
        }

        public List<ResourceGroup> GetResourceGroups()
        {
            List<BackupResourceConfiguration> backupConfigurations = GetBackupConfigurations();
            UploadConfigurations uploadConfigurations = GetUploadConfigurations();
            NotificationConfigs globalNotifications = GetGlobalNotificationConfigs();
            SystemConfigOptions systemConfigOptions = GetSystemConfigOptions();
            List<ResourceGroup> resources = new List<ResourceGroup>();

            foreach (BackupResourceConfiguration resourceConfig in backupConfigurations)
            {
                if (string.IsNullOrWhiteSpace(resourceConfig.Name))
                    continue;

                NotificationConfigs effectiveNotifications = resourceConfig.NotificationConfigs ?? globalNotifications ?? new NotificationConfigs();
                ResourceGroup resourceGroup = new ResourceGroup
                {
                    Name = resourceConfig.Name.Trim(),
                    Id = resourceConfig.Name.FormatToUrlStyle(),
                    DbType = NormalizeDbType(resourceConfig.Type),
                    DbServer = "configured-via-connection-string",
                    DbUsername = "configured-via-connection-string",
                    DbPassword = string.Empty,
                    DbPort = 0,
                    ConnectionString = resourceConfig.ConnectionString?.Trim(),
                    MaximumRunningBots = systemConfigOptions.MaxWorkers < 1 ? 1 : systemConfigOptions.MaxWorkers,
                    CompressBackupFiles = systemConfigOptions.AutoCompressToZip,
                    BackupExpiryAgeInDays = systemConfigOptions.BackupExpiryAgeInDays < 1 ? 1 : systemConfigOptions.BackupExpiryAgeInDays,
                    BackupDeliveryConfig = BuildDeliveryConfiguration(resourceConfig.UploadTo, uploadConfigurations),
                    NotifyOnErrorBackups = effectiveNotifications.NotifyOnBackupFailure,
                    NotifyOnErrorBackupDelivery = effectiveNotifications.NotifyOnUploadFailure,
                    NotifyEmailDestinations = effectiveNotifications.NotifyEmailDestinations ?? new List<string>()
                };
                resources.Add(resourceGroup);
            }

            return resources;
        }

        public List<BackupDatabaseInfo> GetDatabases()
        {
            List<BackupResourceConfiguration> backupConfigurations = GetBackupConfigurations();
            List<BackupDatabaseInfo> response = new List<BackupDatabaseInfo>();

            foreach (BackupResourceConfiguration resourceConfig in backupConfigurations)
            {
                if (string.IsNullOrWhiteSpace(resourceConfig.Name))
                    continue;

                string resourceId = resourceConfig.Name.FormatToUrlStyle();
                List<BackupResourceDatabaseConfiguration> databases = resourceConfig.Databases ?? new List<BackupResourceDatabaseConfiguration>();
                foreach (BackupResourceDatabaseConfiguration database in databases)
                {
                    if (string.IsNullOrWhiteSpace(database.Name))
                        continue;

                    BackupDatabaseInfo backupDatabaseInfo = new BackupDatabaseInfo
                    {
                        Id = $"{resourceId}:{database.Name.Trim()}".ToMD5String().ToUpper(),
                        ResourceGroupId = resourceId,
                        DatabaseName = database.Name.Trim(),
                        Description = $"Configured from appsettings ({resourceConfig.Name})",
                        DateRegisteredUTC = DateTime.UtcNow
                    };
                    response.Add(backupDatabaseInfo);
                }
            }
            return response;
        }

        public List<BackupSchedule> GetSchedules()
        {
            List<BackupResourceConfiguration> backupConfigurations = GetBackupConfigurations();
            List<BackupSchedule> schedules = new List<BackupSchedule>();

            foreach (BackupResourceConfiguration resourceConfig in backupConfigurations)
            {
                if (string.IsNullOrWhiteSpace(resourceConfig.Name))
                    continue;
                string resourceId = resourceConfig.Name.FormatToUrlStyle();

                foreach (BackupResourceDatabaseConfiguration databaseConfig in resourceConfig.Databases ?? new List<BackupResourceDatabaseConfiguration>())
                {
                    if (string.IsNullOrWhiteSpace(databaseConfig.Name))
                        continue;

                    int everyHours = databaseConfig.BackupEveryHrs < 1 ? 1 : databaseConfig.BackupEveryHrs;
                    string dbId = $"{resourceId}:{databaseConfig.Name.Trim()}".ToMD5String().ToUpper();
                    string scheduleId = $"{dbId}:{everyHours}:{databaseConfig.FullBackup}".ToMD5String().ToUpper();
                    BackupSchedule schedule = new BackupSchedule
                    {
                        Id = scheduleId,
                        BackupDatabaseInfoId = dbId,
                        Name = $"{databaseConfig.Name.Trim()} ({everyHours}h)",
                        EveryHours = everyHours,
                        ScheduleType = databaseConfig.FullBackup ? BackupScheduleType.FULLBACKUP.ToString() : BackupScheduleType.DIFFERENTIAL.ToString(),
                        StartDateUTC = DateTime.UtcNow.AddSeconds(-1),
                        CreatedOnUTC = DateTime.UtcNow
                    };
                    schedules.Add(schedule);
                }
            }
            return schedules;
        }

        private static string NormalizeDbType(string type)
        {
            string value = type?.Trim().ToUpper() ?? string.Empty;
            if (value.Contains("MYSQL"))
                return DbTypes.MYSQLDATABASE.ToString();
            if (value.Contains("MARIADB"))
                return DbTypes.MARIADBDATABASE.ToString();
            return DbTypes.SQLSERVER2019.ToString();
        }

        private static BackupDeliveryConfig BuildDeliveryConfiguration(List<string> uploadTo, UploadConfigurations uploadConfigurations)
        {
            HashSet<string> uploadsEnabled = new HashSet<string>((uploadTo ?? new List<string>()).Select(x => x?.Trim()?.ToLower() ?? string.Empty));
            BackupDeliveryConfig backupDeliveryConfig = new BackupDeliveryConfig
            {
                DownloadLink = CopyDownloadLink(uploadConfigurations.DownloadLink),
                Ftp = CopyFtp(uploadConfigurations.Ftp),
                Smtp = CopySmtp(uploadConfigurations.Smtp),
                Dropbox = CopyDropbox(uploadConfigurations.Dropbox),
                ObjectStorage = CopyObjectStorage(uploadConfigurations.ObjectStorage)
            };

            backupDeliveryConfig.DownloadLink.IsEnabled = IsEnabled(uploadsEnabled, "downloadlink", "download");
            backupDeliveryConfig.Ftp.IsEnabled = IsEnabled(uploadsEnabled, "ftp");
            backupDeliveryConfig.Smtp.IsEnabled = IsEnabled(uploadsEnabled, "smtp");
            backupDeliveryConfig.Dropbox.IsEnabled = IsEnabled(uploadsEnabled, "dropbox", "dropdobx");
            backupDeliveryConfig.ObjectStorage.IsEnabled = IsEnabled(uploadsEnabled, "objectstorage", "minio", "s3");
            return backupDeliveryConfig;
        }

        private static bool IsEnabled(HashSet<string> uploadsEnabled, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (uploadsEnabled.Contains(key))
                    return true;
            }
            return false;
        }

        private static DownloadLinkDeliveryConfig CopyDownloadLink(DownloadLinkDeliveryConfig source)
        {
            DownloadLinkDeliveryConfig config = source ?? new DownloadLinkDeliveryConfig();
            return new DownloadLinkDeliveryConfig
            {
                IsEnabled = config.IsEnabled,
                UseShortDownloadLink = config.UseShortDownloadLink
            };
        }

        private static FtpDeliveryConfig CopyFtp(FtpDeliveryConfig source)
        {
            FtpDeliveryConfig config = source ?? new FtpDeliveryConfig();
            return new FtpDeliveryConfig
            {
                IsEnabled = config.IsEnabled,
                Server = config.Server,
                Username = config.Username,
                Password = config.Password,
                Directory = config.Directory
            };
        }

        private static SmtpDeliveryConfig CopySmtp(SmtpDeliveryConfig source)
        {
            SmtpDeliveryConfig config = source ?? new SmtpDeliveryConfig();
            return new SmtpDeliveryConfig
            {
                IsEnabled = config.IsEnabled,
                SMTPEnableSSL = config.SMTPEnableSSL,
                SMTPHost = config.SMTPHost,
                SMTPPort = config.SMTPPort,
                SMTPEmailAddress = config.SMTPEmailAddress,
                SMTPEmailCredentials = config.SMTPEmailCredentials,
                SMTPDefaultSMTPFromName = config.SMTPDefaultSMTPFromName,
                SMTPDestinations = config.SMTPDestinations
            };
        }

        private static DropboxDeliveryConfig CopyDropbox(DropboxDeliveryConfig source)
        {
            DropboxDeliveryConfig config = source ?? new DropboxDeliveryConfig();
            return new DropboxDeliveryConfig
            {
                IsEnabled = config.IsEnabled,
                AccessToken = config.AccessToken,
                Directory = config.Directory
            };
        }

        private static ObjectStorageDeliveryConfig CopyObjectStorage(ObjectStorageDeliveryConfig source)
        {
            ObjectStorageDeliveryConfig config = source ?? new ObjectStorageDeliveryConfig();
            return new ObjectStorageDeliveryConfig
            {
                IsEnabled = config.IsEnabled,
                Server = config.Server,
                Port = config.Port,
                Bucket = config.Bucket,
                AccessKey = config.AccessKey,
                SecretKey = config.SecretKey,
                UseSsl = config.UseSsl
            };
        }
    }
}
