using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
{
    public class ResourceGroupRequest
    {
        [Required]
        public string Name { get; set; }
        public string TimeZone { get; set; } = null;
        [Range(1, 50)]
        public int MaximumBackupRunningThreads { get; set; } = 1;
    }
}
