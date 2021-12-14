using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IResourceGroupPersistanceService
    {
        List<ResourceGroup> GetAll();
        ResourceGroup GetById(string id);
        bool Remove(string id);
        bool Switch(string id);
        bool Add(ResourceGroup record);
        bool Update(ResourceGroup record);
    }
}
