using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.API.Models.Requests
{
    public class ActiveDirectoryRequest
    {
        [Required]
        public string Name { get; set; }
    }
}
