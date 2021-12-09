namespace SemanticBackup.API.SignalRHubs
{
    public class JoinRequest
    {
        public string Directory { get; set; }
        public string Group { get; set; } = "";
    }
}
