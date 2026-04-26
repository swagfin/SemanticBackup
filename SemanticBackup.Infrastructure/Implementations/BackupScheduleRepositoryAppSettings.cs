using Microsoft.Extensions.Configuration;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.Implementations
{
    public class BackupScheduleRepositoryAppSettings : IBackupScheduleRepository
    {
        private readonly AppSettingsConfigurationReader _configurationReader;
        private readonly ConcurrentDictionary<string, DateTime> _runtimeLastRunState;

        public BackupScheduleRepositoryAppSettings(IConfiguration configuration)
        {
            _configurationReader = new AppSettingsConfigurationReader(configuration);
            _runtimeLastRunState = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<List<BackupSchedule>> GetAllAsync(string resourceGroupId)
        {
            List<BackupDatabaseInfo> databases = _configurationReader.GetDatabases()
                .Where(x => x.ResourceGroupId.Equals(resourceGroupId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            HashSet<string> databaseIds = new HashSet<string>(databases.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            List<BackupSchedule> schedules = GetScheduleSnapshot()
                .Where(x => databaseIds.Contains(x.BackupDatabaseInfoId))
                .OrderBy(x => x.Name)
                .ToList();
            return Task.FromResult(schedules);
        }

        public Task<BackupSchedule> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult<BackupSchedule>(null);
            BackupSchedule schedule = GetScheduleSnapshot().FirstOrDefault(x => x.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(schedule);
        }

        public Task<bool> RemoveAsync(string id)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AddOrUpdateAsync(BackupSchedule record)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UpdateAsync(BackupSchedule record)
        {
            return Task.FromResult(false);
        }

        public Task<List<BackupSchedule>> GetAllDueByDateAsync()
        {
            DateTime nowUtc = DateTime.UtcNow;
            List<BackupSchedule> due = GetScheduleSnapshot()
                .Where(x => x.NextRunUTC <= nowUtc)
                .OrderBy(x => x.NextRunUTC)
                .ToList();
            return Task.FromResult(due);
        }

        public Task<List<BackupSchedule>> GetAllByDatabaseIdAsync(string id)
        {
            List<BackupSchedule> schedules = GetScheduleSnapshot()
                .Where(x => x.BackupDatabaseInfoId.Equals(id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Name)
                .ToList();
            return Task.FromResult(schedules);
        }

        public async Task<int> GetAllCountAsync(string resourceGroupId)
        {
            List<BackupSchedule> schedules = await GetAllAsync(resourceGroupId);
            return schedules.Count;
        }

        public Task<bool> UpdateLastRunAsync(string id, DateTime lastRunUTC)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult(false);
            _runtimeLastRunState[id.Trim()] = lastRunUTC;
            return Task.FromResult(true);
        }

        private List<BackupSchedule> GetScheduleSnapshot()
        {
            List<BackupSchedule> schedules = _configurationReader.GetSchedules();
            foreach (BackupSchedule schedule in schedules)
            {
                if (_runtimeLastRunState.TryGetValue(schedule.Id, out DateTime lastRunUtc))
                    schedule.LastRunUTC = lastRunUtc;
            }
            return schedules;
        }
    }
}
