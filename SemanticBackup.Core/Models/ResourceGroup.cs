﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class ResourceGroup
    {
        [Key, Required]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string Key { get { return Name.FormatToUrlStyle(); } }
        [Required]
        public string Name { get; set; }
        public long LastAccess { get; set; } = 0;
        public string TimeZone { get; set; }
        public int MaximumRunningBots { get; set; } = 1;
        public bool CompressBackupFiles { get; set; } = true;
        public int BackupExpiryAgeInDays { get; set; } = 7;
        public bool NotifyOnErrorBackups { get; set; } = false;
        public bool NotifyOnErrorBackupDelivery { get; set; } = false;
        public string NotifyEmailDestinations { get; set; } = null;
    }
}
