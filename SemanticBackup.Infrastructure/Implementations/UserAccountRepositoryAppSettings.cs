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
    public class UserAccountRepositoryAppSettings : IUserAccountRepository
    {
        private readonly AppSettingsConfigurationReader _configurationReader;
        private readonly ConcurrentDictionary<string, (DateTime lastLoginUtc, string token)> _runtimeLoginState;

        public UserAccountRepositoryAppSettings(IConfiguration configuration)
        {
            _configurationReader = new AppSettingsConfigurationReader(configuration);
            _runtimeLoginState = new ConcurrentDictionary<string, (DateTime lastLoginUtc, string token)>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<List<UserAccount>> GetAllAsync()
        {
            List<UserAccount> users = BuildUserAccounts();
            return Task.FromResult(users);
        }

        public Task<UserAccount> GetByEmailAsync(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return Task.FromResult<UserAccount>(null);
            List<UserAccount> users = BuildUserAccounts();
            UserAccount user = users.FirstOrDefault(x => x.EmailAddress.Equals(emailAddress.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<UserAccount> GetByCredentialsAsync(string emailAddress, string password)
        {
            if (string.IsNullOrWhiteSpace(emailAddress) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult<UserAccount>(null);

            List<UserAccount> users = BuildUserAccounts();
            UserAccount user = users.FirstOrDefault(x =>
                x.EmailAddress.Equals(emailAddress.Trim(), StringComparison.OrdinalIgnoreCase)
                && x.Password == password);
            return Task.FromResult(user);
        }

        public Task<bool> RemoveByEmailAsync(string emailAddress)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AddOrUpdateAsync(UserAccount record)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UpdateAsync(UserAccount record)
        {
            return Task.FromResult(false);
        }

        public Task<int> GetAllCountAsync()
        {
            List<UserAccount> users = BuildUserAccounts();
            return Task.FromResult(users.Count);
        }

        public Task<bool> UpdateLastSeenAsync(string emailAddress, DateTime lastSeenUTC, string lastToken)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return Task.FromResult(false);
            _runtimeLoginState[emailAddress.Trim()] = (lastSeenUTC, lastToken ?? string.Empty);
            return Task.FromResult(true);
        }

        public Task<UserAccount> GetByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult<UserAccount>(null);
            List<UserAccount> users = BuildUserAccounts();
            UserAccount user = users.FirstOrDefault(x => x.Id.Equals(userId.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        private List<UserAccount> BuildUserAccounts()
        {
            List<AdminUserConfiguration> configurationUsers = _configurationReader.GetAdminUsers();
            List<UserAccount> users = new List<UserAccount>();
            foreach (AdminUserConfiguration user in configurationUsers)
            {
                if (string.IsNullOrWhiteSpace(user.EmailAddress) || string.IsNullOrWhiteSpace(user.Password))
                    continue;

                string userId = user.EmailAddress.Trim().ToLower();
                UserAccount account = new UserAccount
                {
                    Id = userId,
                    FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.EmailAddress.Trim() : user.FullName.Trim(),
                    EmailAddress = user.EmailAddress.Trim(),
                    Password = user.Password,
                    Timezone = string.IsNullOrWhiteSpace(user.Timezone) ? "Africa/Nairobi" : user.Timezone.Trim(),
                    TimezoneOffset = string.IsNullOrWhiteSpace(user.TimezoneOffset) ? "+03:00" : user.TimezoneOffset.Trim(),
                    UserAccountType = UserAccountType.ADMIN
                };

                if (_runtimeLoginState.TryGetValue(account.EmailAddress, out (DateTime lastLoginUtc, string token) runtimeState))
                {
                    account.LastLoginUTC = runtimeState.lastLoginUtc;
                    account.LastLoginToken = runtimeState.token;
                }

                users.Add(account);
            }
            return users;
        }
    }
}
