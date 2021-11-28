using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Services;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupRecordPersistanceService : IBackupRecordPersistanceService
    {
        private readonly string connString;

        public BackupRecordPersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionString;
        }

        public List<BackupRecord> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().ToList();
            }
        }
        public bool AddOrUpdate(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Upsert(record);
            }
        }

        public BackupRecord GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<BackupRecord>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    return collection.Delete(new BsonValue(objFound.Id));
                return false;
            }
        }

        public bool Update(BackupRecord record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupRecord>().Update(record);
            }
        }
    }
}
