using System;

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
        public string JWTSecret { get; set; } = Guid.NewGuid().ToString();
        public int JWTExpirationInDays { get; set; } = 3;
        public string JWTIssuer { get; set; } = "issuer";
        public string JWTAudience { get; set; } = "audiences";
    }
}
