using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class UserAccount
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string FullName { get; set; }
        [Required]
        public string EmailAddress { get; set; }
        [Required]
        public string Password { get; set; }

        public string TimezoneOffset { get; set; } = "+03:00";
        public string Timezone { get; set; } = "Africa/Nairobi";

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
