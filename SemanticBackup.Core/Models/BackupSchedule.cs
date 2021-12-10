using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupSchedule
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string ScheduleType { get; set; } = BackupScheduleType.FULLBACKUP.ToString();
        public int EveryHours { get; set; } = 24;
        public DateTime StartDateUTC { get; set; } = DateTime.UtcNow;
        public DateTime NextRunUTC
        {
            get
            {
                if (LastRunUTC.Date == new DateTime(2000, 1, 1).Date)
                    return StartDateUTC;
                return ((DateTime)LastRunUTC).AddHours(EveryHours);
            }
        }
        public DateTime LastRunUTC { get; set; } = new DateTime(2000, 1, 1).Date;
        public DateTime CreatedOnUTC { get; set; } = DateTime.UtcNow;
    }
    public enum BackupScheduleType
    {
        FULLBACKUP,
        DIFFERENTIAL
    }
}
