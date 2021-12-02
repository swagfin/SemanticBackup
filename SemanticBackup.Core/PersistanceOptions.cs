namespace SemanticBackup.Core
{
    public class PersistanceOptions
    {
        public string ServerDefaultTimeZone { get; set; } = "E. Africa Standard Time";
        public int DefaultBackupExpiryAgeInDays { get; set; } = 7;
        public int MaximumBackupRunningThreads { get; set; } = 10;
        public string DefaultBackupDirectory { get; set; } = "c:\\backups\\";
        public bool EnsureDefaultBackupDirectoryExists { get; set; } = true;
        public string BackupFileSaveFormat { get; set; } = "{{database}}\\{{database}}-{{datetime}}.{{databasetype}}.bak";
        public bool CompressBackupFiles { get; set; } = true;
    }
}
