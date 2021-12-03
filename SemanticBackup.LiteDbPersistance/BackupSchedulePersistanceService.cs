using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class BackupSchedulePersistanceService : IBackupSchedulePersistanceService
    {
        private readonly ConnectionString connString;

        public BackupSchedulePersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }

        public List<BackupSchedule> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Query().ToList();
            }
        }
        public List<BackupSchedule> GetAllDueByDate(DateTime dateTime)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Query().Where(x => x.NextRun >= dateTime).OrderBy(x => x.NextRun).ToList();
            }
        }
        public List<BackupSchedule> GetAllByDatabaseId(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Query().Where(x => x.BackupDatabaseInfoId == id).ToList();
            }
        }
        public bool AddOrUpdate(BackupSchedule record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Upsert(record);
            }
        }

        public BackupSchedule GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Query().Where(x => x.Id == id).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<BackupSchedule>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    return collection.Delete(new BsonValue(objFound.Id));
                return false;
            }
        }

        public bool Update(BackupSchedule record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<BackupSchedule>().Update(record);
            }
        }
    }
}
