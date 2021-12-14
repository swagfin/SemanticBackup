using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IContentDeliveryConfigPersistanceService
    {
        List<ContentDeliveryConfiguration> GetAll(string resourceGroupId);
        ContentDeliveryConfiguration GetById(string id);
        bool Remove(string id);
        bool AddOrUpdate(ContentDeliveryConfiguration record);
        bool Update(ContentDeliveryConfiguration record);
        bool AddOrUpdate(List<ContentDeliveryConfiguration> records);
        bool RemoveAllByResourceGroup(string resourceGroupId);
    }
}
