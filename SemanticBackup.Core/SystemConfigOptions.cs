namespace SemanticBackup.Core
{
    public class SystemConfigOptions
    {
        public string DefaultBackupDirectory { get; set; } = "c:\\backups\\";
        public string BackupFileSaveFormat { get; set; } = "{{database}}\\{{database}}-{{datetime}}.{{databasetype}}.bak";
        public int ExecutionTimeoutInMinutes { get; set; } = 10;
        public bool InDepthBackupRecordDeleteEnabled { get; set; } = true;

        public string SMTPEmailAddress { get; set; } = null;
        public string SMTPEmailCredentials { get; set; }
        public int SMTPPort { get; set; }
        public bool SMTPEnableSSL { get; set; } = true;
        public string SMTPHost { get; set; } = null;
        public string SMTPDefaultSMTPFromName { get; set; }
    }
}
