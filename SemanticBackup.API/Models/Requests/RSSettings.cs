namespace SemanticBackup.API.Models.Requests
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
    }
    public class RSDropBoxSettings
    {
        public bool IsEnabled { get; set; } = false;
        public string AccessToken { get; set; }
        public string Directory { get; set; } = "/";
    }
    public class RSAzureBlobStorageSettings
    {
        public bool IsEnabled { get; set; } = false;
        public string ConnectionString { get; set; }
        public string BlobContainer { get; set; }
    }
}
