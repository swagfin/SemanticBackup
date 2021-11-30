namespace SemanticBackup.WebClient
{
    public class WebClientOptions
    {
        public string WebApiUrl { get; set; } = "https://localhost:5001";
        public string SigningSecret { get; set; } = "!!unsecuredJwt!!";
    }
}
