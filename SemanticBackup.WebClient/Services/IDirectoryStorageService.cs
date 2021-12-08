using System;
using System.Collections.Generic;

namespace SemanticBackup.WebClient.Services
{
    public interface IDirectoryStorageService
    {
        void InitDirectories();
        List<ActiveDirectory> GetActiveDirectories();
        ActiveDirectory GetActiveDirectory(string id);
        bool RemoveDirectory(string id);
        bool SwitchToDirectory(string id);
        bool SwitchToDirectory(ActiveDirectory directory);
        bool AddDirectory(ActiveDirectory apiDirectory);
        bool UpdateDirectory(ActiveDirectory apiDirectory);
    }
}
namespace SemanticBackup.WebClient
{
    public class ActiveDirectory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string Name { get; set; }
        public string Url { get; set; }
        public long LastAccess { get; set; } = 0;
    }
}
