using LiteDB.Async;

namespace SemanticBackup.LiteDbPersistance
{
    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabaseAsync Database { get; }
        public LiteDbContext(LiteDbPersistanceOptions liteDbPersistanceOptions)
        {
            Database = new LiteDatabaseAsync(liteDbPersistanceOptions.ConnectionStringLiteDb);
            //Set Configurations
            Database.PragmaAsync("UTC_DATE", true).GetAwaiter().GetResult();
        }
    }
    public interface ILiteDbContext
    {
        LiteDatabaseAsync Database { get; }
    }
}
