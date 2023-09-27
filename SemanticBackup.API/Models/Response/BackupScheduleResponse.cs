using System;

namespace SemanticBackup.Core.Models.Response
{
    public class BackupScheduleResponse
    {
        public string Id { get; set; }
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string ScheduleType { get; set; }
        public int EveryHours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextRun { get; set; }
        public DateTime? LastRun { get; set; } = null;
    }
}
