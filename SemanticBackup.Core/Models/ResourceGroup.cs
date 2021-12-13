using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class ResourceGroup
    {
        [Key, Required]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string Name { get; set; }
        public long LastAccess { get; set; } = 0;
        public string TimeZone { get; set; }
        public int MaximumRunningBots { get; set; } = 1;
        public bool CompressBackupFiles { get; set; } = true;
    }
}
