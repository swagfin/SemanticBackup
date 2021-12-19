using LiteDB;
using LiteDB.Async;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.LiteDbPersistance
{
    public class ContentDeliveryConfigPersistanceService : IContentDeliveryConfigPersistanceService
    {
        private LiteDatabaseAsync db;

        public ContentDeliveryConfigPersistanceService(ILiteDbContext context)
        {
            this.db = context.Database;
        }

        public async Task<List<ContentDeliveryConfiguration>> GetAllAsync(string resourceGroupId)
        {
            return await db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.PriorityIndex).ToListAsync();
        }
        public async Task<bool> AddOrUpdateAsync(ContentDeliveryConfiguration record)
        {
            if (record == null)
                return false;
            //Check Existing Type
            List<ContentDeliveryConfiguration> existingDeliveryTypes = await db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.DeliveryType == record.DeliveryType && x.ResourceGroupId == record.ResourceGroupId).OrderBy(x => x.DeliveryType).ToListAsync();
            if (existingDeliveryTypes != null && existingDeliveryTypes.Count > 0)
                foreach (ContentDeliveryConfiguration existingRecord in existingDeliveryTypes)
                    await RemoveAsync(existingRecord.Id);
            //Proceed and Save New Record
            return await db.GetCollection<ContentDeliveryConfiguration>().UpsertAsync(record);
        }
        public async Task<bool> RemoveAllByResourceGroupAsync(string resourceGroupId)
        {
            bool successAll = false;
            List<ContentDeliveryConfiguration> existingDeliveryTypes = await db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.ResourceGroupId == resourceGroupId).OrderBy(x => x.DeliveryType).ToListAsync();
            if (existingDeliveryTypes != null && existingDeliveryTypes.Count > 0)
                foreach (ContentDeliveryConfiguration existingRecord in existingDeliveryTypes)
                    successAll = await RemoveAsync(existingRecord.Id);
            return successAll;
        }
        public async Task<bool> AddOrUpdateAsync(List<ContentDeliveryConfiguration> records)
        {
            if (records == null || records.Count < 1)
                return false;
            bool success = false;
            foreach (var record in records)
                success = await AddOrUpdateAsync(record);
            return success;
        }

        public async Task<ContentDeliveryConfiguration> GetByIdAsync(string id)
        {
            return await db.GetCollection<ContentDeliveryConfiguration>().Query().Where(x => x.Id == id).OrderBy(x => x.DeliveryType).FirstOrDefaultAsync();
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var collection = db.GetCollection<ContentDeliveryConfiguration>();
            var objFound = await collection.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (objFound != null)
                return await collection.DeleteAsync(new BsonValue(objFound.Id));
            return false;
        }

        public async Task<bool> UpdateAsync(ContentDeliveryConfiguration record)
        {
            return await db.GetCollection<ContentDeliveryConfiguration>().UpdateAsync(record);
        }
    }
}
