using SemanticBackup.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
{
    public class BackupScheduleRequest
    {
        [Required]
        public string BackupDatabaseInfoId { get; set; }
        public string ScheduleType { get; set; } = BackupScheduleType.FULLBACKUP.ToString();
        public int EveryHours { get; set; } = 24;
        public DateTime StartDate { get; set; } = DateTime.Now;
    }
}
