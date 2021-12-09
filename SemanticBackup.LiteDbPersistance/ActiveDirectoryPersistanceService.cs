using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class ResourceGroupPersistanceService : IResourceGroupPersistanceService
    {
        private readonly ConnectionString connString;

        public ResourceGroupPersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }
        public bool Add(ResourceGroup record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<ResourceGroup>().Upsert(record);
            }
        }

        public List<ResourceGroup> GetAll()
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<ResourceGroup>().Query().OrderBy(x => x.Name).ToList();
            }
        }

        public ResourceGroup GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<ResourceGroup>().Query().Where(x => x.Id == id).OrderBy(x => x.Name).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<ResourceGroup>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    return collection.Delete(new BsonValue(objFound.Id));
                return false;
            }
        }

        public bool Switch(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                var collection = db.GetCollection<ResourceGroup>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    if (long.TryParse(DateTime.Now.ToString("yyyyMMddHHmmss"), out long lastAccess))
                    {
                        objFound.LastAccess = lastAccess;
                        bool updatedSuccess = collection.Update(objFound);
                        return updatedSuccess;
                    }
                return false;
            }

        }

        public bool Update(ResourceGroup record)
        {
            using (var db = new LiteDatabase(connString))
            {
                return db.GetCollection<ResourceGroup>().Update(record);
            }
        }
    }
}
