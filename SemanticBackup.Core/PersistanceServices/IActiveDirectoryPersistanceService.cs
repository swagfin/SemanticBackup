using SemanticBackup.Core.Models;
using System.Collections.Generic;

namespace SemanticBackup.Core.PersistanceServices
{
    public interface IActiveDirectoryPersistanceService
    {
        List<ActiveDirectory> GetAll();
        ActiveDirectory GetById(string id);
        bool Remove(string id);
        bool Switch(string id);
        bool Add(ActiveDirectory apiDirectory);
        bool Update(ActiveDirectory apiDirectory);
    }
}
