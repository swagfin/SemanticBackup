namespace SemanticBackup.LiteDbPersistance
{
    public class LiteDbPersistanceOptions
    {
        public string ConnectionString { get; set; } = "{{env}}\\data\\database.db";
    }
}
