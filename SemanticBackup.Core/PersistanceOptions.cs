namespace SemanticBackup.Core
{
    public class PersistanceOptions
    {
        public string ServerDefaultTimeZone { get; set; } = "E. Africa Standard Time";
        public string DefaultBackupDirectory { get; set; } = "c:\\backups\\";
        public bool EnsureDefaultBackupDirectoryExists { get; set; } = true;
        public string BackupFileSaveFormat { get; set; } = "{{database}}\\{{database}}-{{datetime}}.{{databasetype}}.bak";
    }
}
