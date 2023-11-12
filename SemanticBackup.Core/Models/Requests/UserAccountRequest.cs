using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models.Requests
{
    public class UserAccountRequest
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string EmailAddress { get; set; }

        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = null;

        [DataType(DataType.Password), Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = null;

        [Required]
        public string TimezoneId { get; set; }
    }
}
