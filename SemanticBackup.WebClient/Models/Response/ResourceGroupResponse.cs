namespace SemanticBackup.WebClient.Models.Response
{
    public class ResourceGroupResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long LastAccess { get; set; }
        public string TimeZone { get; set; } = null;
        public int MaximumRunningBots { get; set; }
        public bool CompressBackupFiles { get; set; }
        public int BackupExpiryAgeInDays { get; set; }
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
    }
}
