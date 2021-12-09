using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services.Implementations
{
    public class DirectoryStorageService : IDirectoryStorageService
    {
        public string ApiUrl { get; }

        private readonly IHttpService _httpService;
        private readonly ILogger<DirectoryStorageService> _logger;

        public string DirectorySavingFile { get; }
        public DirectoryStorageService(IOptions<WebClientOptions> options, IHttpService httpService, ILogger<DirectoryStorageService> logger)
        {
            this.ApiUrl = options.Value.ApiUrl;
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task ReloadTempDirectories(List<ActiveDirectoryResponse> activeDirectoryResponses = null)
        {
            try
            {
                if (activeDirectoryResponses == null)
                    Directories.ActiveDirectories = await GetAllAsync();
                else
                    Directories.ActiveDirectories = activeDirectoryResponses;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
        }
        public async Task<List<ActiveDirectoryResponse>> GetAllAsync()
        {
            var url = "api/ActiveDirectories/";
            var directories = await _httpService.GetAsync<List<ActiveDirectoryResponse>>(url);
            await ReloadTempDirectories(directories);
            return directories;
        }
        public async Task<bool> AddAsync(ActiveDirectoryRequest apiDirectory)
        {
            var url = "api/ActiveDirectories/";
            bool success = await _httpService.PostAsync<bool>(url, apiDirectory);
            await ReloadTempDirectories();
            return success;
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var url = $"api/ActiveDirectories/{id}";
            bool success = await _httpService.DeleteAsync<bool>(url);
            await ReloadTempDirectories();
            return success;
        }

        public async Task<bool> UpdateAsync(ActiveDirectoryRequest apiDirectory)
        {
            var url = "api/ActiveDirectories/";
            bool success = await _httpService.PutAsync<bool>(url, apiDirectory);
            await ReloadTempDirectories();
            return success;
        }

        public async Task<bool> SwitchAsync(string id)
        {
            var url = $"api/ActiveDirectories/switch-directory/{id}";
            bool success = await _httpService.GetAsync<bool>(url);
            await ReloadTempDirectories();
            return success;
        }

        public async Task<ActiveDirectoryResponse> GetByIdAsync(string id)
        {
            var url = $"api/ActiveDirectories/{id}";
            return await _httpService.GetAsync<ActiveDirectoryResponse>(url);
        }
    }
}
