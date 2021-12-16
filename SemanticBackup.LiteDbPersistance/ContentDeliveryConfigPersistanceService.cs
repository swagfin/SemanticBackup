using LiteDB;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System.Collections.Generic;

namespace SemanticBackup.LiteDbPersistance
{
    public class ContentDeliveryConfigPersistanceService : IContentDeliveryConfigPersistanceService
    {
        private readonly ConnectionString connString;

        public ContentDeliveryConfigPersistanceService(LiteDbPersistanceOptions options)
        {
            this.connString = options.ConnectionStringLiteDb;
        }

        public List<ContentDeliveryConfiguration> GetAll(string resourceGroupId)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.PriorityIndex).ToList();
            }
        }
        public bool AddOrUpdate(ContentDeliveryConfiguration record)
        {
            if (record == null)
                return false;
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                //Check Existing Type
                List<ContentDeliveryConfiguration> existingDeliveryTypes = db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.DeliveryType == record.DeliveryType && x.ResourceGroupId == record.ResourceGroupId).OrderBy(x => x.DeliveryType).ToList();
                if (existingDeliveryTypes != null && existingDeliveryTypes.Count > 0)
                    foreach (ContentDeliveryConfiguration existingRecord in existingDeliveryTypes)
                        Remove(existingRecord.Id);
                //Proceed and Save New Record
                return db.GetCollection<ContentDeliveryConfiguration>().Upsert(record);
            }
        }
        public bool RemoveAllByResourceGroup(string resourceGroupId)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                bool successAll = false;
                List<ContentDeliveryConfiguration> existingDeliveryTypes = db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.DeliveryType).ToList();
                if (existingDeliveryTypes != null && existingDeliveryTypes.Count > 0)
                    foreach (ContentDeliveryConfiguration existingRecord in existingDeliveryTypes)
                        successAll = Remove(existingRecord.Id);
                return successAll;
            }
        }
        public bool AddOrUpdate(List<ContentDeliveryConfiguration> records)
        {
            if (records == null || records.Count < 1)
                return false;
            bool success = false;
            foreach (var record in records)
                success = AddOrUpdate(record);
            return success;
        }

        public ContentDeliveryConfiguration GetById(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.Id == id).OrderBy(x => x.DeliveryType).FirstOrDefault();
            }
        }

        public bool Remove(string id)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                var collection = db.GetCollection<ContentDeliveryConfiguration>();
                var objFound = collection.Query().Where(x => x.Id == id).FirstOrDefault();
                if (objFound != null)
                    return collection.Delete(new BsonValue(objFound.Id));
                return false;
            }
        }

        public bool Update(ContentDeliveryConfiguration record)
        {
            using (var db = new LiteDatabase(connString))
            {
                db.Pragma("UTC_DATE", true);
                return db.GetCollection<ContentDeliveryConfiguration>().Update(record);
            }
        }
    }
}
