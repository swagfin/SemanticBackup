using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.WebClient.Models.Requests
{
    public class ActiveDirectoryRequest
    {
        [Required]
        public string Name { get; set; }
    }
}
