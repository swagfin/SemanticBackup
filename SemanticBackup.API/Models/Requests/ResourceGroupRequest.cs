using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
{
    public class ResourceGroupRequest
    {
        [Required]
        public string Name { get; set; }
        public string TimeZone { get; set; } = null;
    }
}
