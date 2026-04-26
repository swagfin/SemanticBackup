using Microsoft.Extensions.Configuration;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class DatabaseInfoRepositoryAppSettings : IDatabaseInfoRepository
    {
        private readonly AppSettingsConfigurationReader _configurationReader;

        public DatabaseInfoRepositoryAppSettings(IConfiguration configuration)
        {
            _configurationReader = new AppSettingsConfigurationReader(configuration);
        }

        public Task<List<BackupDatabaseInfo>> GetAllAsync(string resourceGroupId)
        {
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            databases = databases.Where(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DatabaseName).ToList();
            return Task.FromResult(databases);
        }

        public Task<BackupDatabaseInfo> GetByIdAsync(string databaseIdentifier)
        {
            if (string.IsNullOrWhiteSpace(databaseIdentifier))
                return Task.FromResult<BackupDatabaseInfo>(null);
            string identity = databaseIdentifier.Trim();
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            BackupDatabaseInfo record = databases.FirstOrDefault(x => x.Id.Equals(identity, StringComparison.OrdinalIgnoreCase) || x.DatabaseName.Equals(identity, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(record);
        }

        public Task<bool> RemoveAsync(string id)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AddOrUpdateAsync(BackupDatabaseInfo record)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UpdateAsync(BackupDatabaseInfo record)
        {
            return Task.FromResult(false);
        }

        public Task<int> GetAllCountAsync(string resourceGroupId)
        {
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            int count = databases.Count(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(count);
        }

        public Task<BackupDatabaseInfo> VerifyDatabaseInResourceGroupThrowIfNotExistAsync(string resourceGroupId, string databaseIdentifier)
        {
            if (string.IsNullOrWhiteSpace(resourceGroupId) || string.IsNullOrWhiteSpace(databaseIdentifier))
                throw new Exception($"unknown database with identity key {databaseIdentifier} under resource group id: {resourceGroupId}");

            string identity = databaseIdentifier.Trim();
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            BackupDatabaseInfo record = databases.FirstOrDefault(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase)
                && (x.Id.Equals(identity, StringComparison.OrdinalIgnoreCase) || x.DatabaseName.Equals(identity, StringComparison.OrdinalIgnoreCase)));

            if (record == null)
                throw new Exception($"unknown database with identity key {databaseIdentifier} under resource group id: {resourceGroupId}");
            return Task.FromResult(record);
        }

        public Task<List<string>> GetDatabaseIdsForResourceGroupAsync(string resourceGroupId)
        {
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            List<string> ids = databases
                .Where(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .ToList();
            return Task.FromResult(ids);
        }

        public Task<List<string>> GetDatabaseNamesForResourceGroupAsync(string resourceGroupId)
        {
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases();
            List<string> names = databases.Where(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase)).Select(x => x.DatabaseName).ToList();
            return Task.FromResult(names);
        }
    }
}
