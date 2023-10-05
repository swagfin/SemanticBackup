using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupDatabaseInfo
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ResourceGroupId { get; set; }
        [Required, MinLength(3)]
        public string DatabaseName { get; set; }
        public string Description { get; set; }
        public DateTime DateRegisteredUTC { get; set; } = DateTime.UtcNow;
    }
}
