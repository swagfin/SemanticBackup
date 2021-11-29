using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupSchedule
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string ScheduleType { get; set; } = BackupScheduleType.FULLBACKUP.ToString();
        public int EveryHours { get; set; } = 24;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime NextRun { get { return StartDate.AddHours(EveryHours); } }
        public DateTime? LastRun { get; set; } = null;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
    public enum BackupScheduleType
    {
        FULLBACKUP,
        DIFFERENTIAL
    }
}
