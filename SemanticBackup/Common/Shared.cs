using SemanticBackup.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.Common
{
    public class Shared
    {
        public static List<ResourceGroup> All { get; set; } = new List<ResourceGroup>();
        public static ResourceGroup CurrentResourceGroup
        {
            get
            {
                return All.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
            }
        }
    }
}
