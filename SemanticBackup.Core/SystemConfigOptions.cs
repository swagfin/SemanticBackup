using System;

namespace SemanticBackup.Core
{
    public class SystemConfigOptions
    {
        public string ServerDefaultTimeZone { get; set; } = "E. Africa Standard Time";
        public string DefaultBackupDirectory { get; set; } = "c:\\backups\\";
        public bool EnsureDefaultBackupDirectoryExists { get; set; } = true;
        public string BackupFileSaveFormat { get; set; } = "{{database}}\\{{database}}-{{datetime}}.{{databasetype}}.bak";
        public int ExecutionTimeoutInMinutes { get; set; } = 10;
        public bool InDepthBackupRecordDeleteEnabled { get; set; } = true;

        public string SMTPEmailAddress { get; set; } = null;
        public string SMTPEmailCredentials { get; set; }
        public int SMTPPort { get; set; }
        public bool SMTPEnableSSL { get; set; } = true;
        public string SMTPHost { get; set; } = null;
        public string SMTPDefaultSMTPFromName { get; set; }
        public string JWTSecret { get; set; } = Guid.NewGuid().ToString();
        public int JWTExpirationInDays { get; set; } = 3;
        public string JWTIssuer { get; set; } = "issuer";
        public string JWTAudience { get; set; } = "audiences";
        public string PublicAccessToken { get; set; } = Guid.NewGuid().ToString();
        public bool IsLinuxEnv { get; set; }
    }
}
