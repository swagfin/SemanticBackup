using Microsoft.Extensions.Configuration;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class ResourceGroupRepositoryAppSettings : IResourceGroupRepository
    {
        private readonly AppSettingsConfigurationReader _configurationReader;

        public ResourceGroupRepositoryAppSettings(IConfiguration configuration)
        {
            _configurationReader = new AppSettingsConfigurationReader(configuration);
        }

        public Task<List<ResourceGroup>> GetAllAsync()
        {
            List<ResourceGroup> groups = _configurationReader.GetResourceGroups();
            groups = groups.OrderBy(x => x.Name).ToList();
            return Task.FromResult(groups);
        }

        public Task<ResourceGroup> GetByIdOrKeyAsync(string resourceGroupIdentifier)
        {
            if (string.IsNullOrWhiteSpace(resourceGroupIdentifier))
                return Task.FromResult<ResourceGroup>(null);

            string identity = resourceGroupIdentifier.Trim();
            List<ResourceGroup> groups = _configurationReader.GetResourceGroups();
            ResourceGroup resourceGroup = groups.FirstOrDefault(x =>
                x.Id.Equals(identity, StringComparison.OrdinalIgnoreCase)
                || x.Key.Equals(identity, StringComparison.OrdinalIgnoreCase)
                || x.Name.Equals(identity, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(resourceGroup);
        }

        public Task<bool> RemoveAsync(string resourceGroupIdentifier)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AddAsync(ResourceGroup record)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UpdateAsync(ResourceGroup record)
        {
            return Task.FromResult(false);
        }

        public async Task<ResourceGroup> VerifyByIdOrKeyThrowIfNotExistAsync(string resourceGroupIdentifier)
        {
            ResourceGroup response = await GetByIdOrKeyAsync(resourceGroupIdentifier);
            if (response == null)
                throw new Exception($"resource group not found with provided identity: {resourceGroupIdentifier}");
            return response;
        }
    }
}
