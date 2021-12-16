namespace SemanticBackup.API
{
    public class ApiConfigOptions
    {
        public string SMTPEmailAddress { get; set; } = null;
        public string SMTPEmailCredentials { get; set; }
        public int SMTPPort { get; set; }
        public bool SMTPEnableSSL { get; set; } = true;
        public string SMTPHost { get; set; } = null;
        public string SMTPDefaultSMTPFromName { get; set; }
    }
}
