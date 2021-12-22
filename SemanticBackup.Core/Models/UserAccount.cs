using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class UserAccount
    {
        [Key]
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public UserAccountType UserAccountType { get; set; } = UserAccountType.ADMIN;
        public DateTime? LastLoginUTC { get; set; } = null;
        public string LastLoginToken { get; set; } = null;
    }
    public enum UserAccountType
    {
        ADMIN = 1,
        USER = 2
    }
}
