using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services.Implementations
{
    public class ResourceGroupService : IResourceGroupService
    {
        public string ApiUrl { get; }

        private readonly IHttpService _httpService;
        private readonly ILogger<ResourceGroupService> _logger;

        public string DirectorySavingFile { get; }
        public ResourceGroupService(IOptions<WebClientOptions> options, IHttpService httpService, ILogger<ResourceGroupService> logger)
        {
            this.ApiUrl = options.Value.ApiUrl;
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task ReloadTempResourceGroups(List<ResourceGroupResponse> records = null)
        {
            try
            {
                if (records == null)
                    ResourceGroups.All = await GetAllAsync();
                else
                    ResourceGroups.All = records;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
        }
        public async Task<List<ResourceGroupResponse>> GetAllAsync()
        {
            var url = "api/ResourceGroups/";
            var records = await _httpService.GetAsync<List<ResourceGroupResponse>>(url);
            await ReloadTempResourceGroups(records);
            return records;
        }
        public async Task<bool> AddAsync(ResourceGroupRequest record)
        {
            var url = "api/ResourceGroups/";
            bool success = await _httpService.PostAsync<bool>(url, record);
            await ReloadTempResourceGroups();
            return success;
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var url = $"api/ResourceGroups/{id}";
            bool success = await _httpService.DeleteAsync<bool>(url);
            await ReloadTempResourceGroups();
            return success;
        }

        public async Task<bool> UpdateAsync(ResourceGroupRequest record)
        {
            var url = "api/ResourceGroups/";
            bool success = await _httpService.PutAsync<bool>(url, record);
            await ReloadTempResourceGroups();
            return success;
        }

        public async Task<bool> SwitchAsync(string id)
        {
            var url = $"api/ResourceGroups/switch-resource-group/{id}";
            bool success = await _httpService.GetAsync<bool>(url);
            await ReloadTempResourceGroups();
            return success;
        }

        public async Task<ResourceGroupResponse> GetByIdAsync(string id)
        {
            var url = $"api/ResourceGroups/{id}";
            return await _httpService.GetAsync<ResourceGroupResponse>(url);
        }
    }
}
