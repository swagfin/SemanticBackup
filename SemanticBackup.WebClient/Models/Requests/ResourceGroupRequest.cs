using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.WebClient.Models.Requests
{
    public class ResourceGroupRequest
    {
        [Required]
        public string Name { get; set; }
        public string TimeZone { get; set; } = null;
        public int MaximumBackupRunningThreads { get; set; } = 1;
    }
}
