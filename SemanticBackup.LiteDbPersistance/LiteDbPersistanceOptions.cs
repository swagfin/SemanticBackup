using LiteDB;

namespace SemanticBackup.LiteDbPersistance
{
    public class LiteDbPersistanceOptions
    {
        public string ConnectionString { get; set; } = "{{env}}\\data\\database.db";
        public ConnectionString ConnectionStringLiteDb
        {
            get
            {
                return new ConnectionString(ConnectionString)
                {
                    Password = "12345678",
                    Connection = ConnectionType.Shared
                };
            }
        }
    }
}
