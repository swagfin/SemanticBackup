using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models.Requests
{
    public class DatabaseInfoRequest
    {
        [Required]
        public string DatabaseName { get; set; }
        public string Description { get; set; }
        public bool AutoCreateSchedule { get; set; } = true;
    }
}
