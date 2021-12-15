using System.Collections.Generic;

namespace SemanticBackup.Core.Models
{
    public class RSDownloadLinkSetting
    {
        public bool IsEnabled { get; set; } = true;
        public string DownloadLinkType { get; set; }
    }
    public class RSFTPSetting
    {
        public bool IsEnabled { get; set; } = false;
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Directory { get; set; } = "/";
    }
    public class RSEmailSMTPSetting
    {
        public bool IsEnabled { get; set; } = false;
        public bool SMTPEnableSSL { get; set; } = true;
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; } = 587;
        public string SMTPEmailAddress { get; set; }
        public string SMTPEmailCredentials { get; set; }
        public string SMTPDefaultSMTPFromName { get; set; }
        public string SMTPDestinations { get; set; }
        public List<string> ValidSMTPDestinations
        {
            get
            {
                List<string> allEmails = new List<string>();
                if (SMTPDestinations == null)
                    return allEmails;
                string[] emailSplits = SMTPDestinations?.Split(',');
                if (emailSplits.Length < 1)
                    return allEmails;
                foreach (string email in emailSplits)
                    if (!string.IsNullOrEmpty(email))
                        allEmails.Add(email.Replace(" ", string.Empty).Trim());
                return allEmails;
            }
        }
    }

    public class RSDropBoxSettings
    {
        public bool IsEnabled { get; set; } = false;
        public string AccessToken { get; set; }
        public string Directory { get; set; } = "/";
    }
}
