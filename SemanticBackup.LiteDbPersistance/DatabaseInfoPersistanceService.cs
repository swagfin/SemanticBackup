using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class DatabaseInfoPersistanceService : IDatabaseInfoPersistanceService
    {
        private readonly ConnectionString connString;

        public DatabaseInfoPersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }

        public List<BackupDatabaseInfo> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupDatabaseInfo>().Query().ToList();
            }
        }
        public bool AddOrUpdate(BackupDatabaseInfo record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupDatabaseInfo>().Upsert(record);
            }
        }

        public BackupDatabaseInfo GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupDatabaseInfo>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<BackupDatabaseInfo>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    return collection.Delete(new BsonValue(objFound.Id));
                return false;
            }
        }

        public bool Update(BackupDatabaseInfo record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupDatabaseInfo>().Update(record);
            }
        }
    }
}
