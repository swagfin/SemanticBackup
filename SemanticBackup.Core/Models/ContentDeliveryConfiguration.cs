﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class ContentDeliveryConfiguration
    {
        [Key, Required]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        [Required]
        public string ResourceGroupId { get; set; }
        [Required]
        public string DeliveryType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Configuration { get; set; } = "[]";
    }

    public enum ContentDeliveryType
    {
        DOWNLOAD_LINK,
        FTP_UPLOAD,
        EMAIL_SMTP
    }
}