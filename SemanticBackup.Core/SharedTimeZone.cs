using SemanticBackup.Core;
using SemanticBackup.Core.PersistanceServices;

namespace SemanticBackup.API.Core
{
    public class SharedTimeZone
    {
        private readonly IResourceGroupPersistanceService resourceGroupPersistanceService;
        public string DefaultTimezone { get; }

        public SharedTimeZone(PersistanceOptions persistanceOptions, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this.resourceGroupPersistanceService = resourceGroupPersistanceService;
            this.DefaultTimezone = string.IsNullOrWhiteSpace(persistanceOptions.ServerDefaultTimeZone) ? "GMT Standard Time" : persistanceOptions.ServerDefaultTimeZone;
        }

    }
}
