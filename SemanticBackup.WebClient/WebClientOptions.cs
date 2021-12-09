namespace SemanticBackup.WebClient
{
    public class WebClientOptions
    {
        public string ApiUrl { get; set; }
        public string SigningSecret { get; set; } = "!unsecured!";
    }
}
