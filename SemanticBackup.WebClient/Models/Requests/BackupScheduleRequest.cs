using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.WebClient.Models.Requests
{
    public class BackupScheduleRequest
    {
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string ScheduleType { get; set; }
        public int EveryHours { get; set; } = 24;
        public DateTime StartDate { get; set; } = DateTime.Now;
    }

    public enum BackupScheduleType
    {
        FULLBACKUP,
        DIFFERENTIAL
    }
}
