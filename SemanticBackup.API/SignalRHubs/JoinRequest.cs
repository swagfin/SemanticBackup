namespace SemanticBackup.API.SignalRHubs
{
    public class JoinRequest
    {
        public string Resourcegroup { get; set; }
        public string Group { get; set; } = "";
    }
}
