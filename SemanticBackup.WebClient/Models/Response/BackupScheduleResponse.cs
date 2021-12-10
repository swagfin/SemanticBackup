using System;

namespace SemanticBackup.WebClient.Models.Response
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
        public DateTime LastRun { get; set; }
        public string LastRunPreviewable
        {
            get
            {
                if (LastRun.Date == new DateTime(2000, 1, 1).Date)
                    return "Never";
                else
                    return string.Format("{0:yyyy-MM-dd hh:mm tt}", LastRun);
            }
        }
    }
}
