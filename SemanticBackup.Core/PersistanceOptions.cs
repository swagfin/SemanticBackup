namespace SemanticBackup.Core
{
    public class PersistanceOptions
    {
        public string ServerDefaultTimeZone { get; set; } = "E. Africa Standard Time";
        public int DefaultBackupExpiryAgeInDays { get; set; } = 7;
        public string DefaultBackupDirectory { get; set; } = "c:\\backups\\";
    }
}
