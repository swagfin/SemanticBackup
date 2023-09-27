using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.DatabaseBackups
{
    [Authorize]
    public class InfoModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;
        private readonly IResourceGroupRepository _resourceGroupPersistanceService;

        public string ApiEndPoint { get; }
        public BackupRecord BackupRecordResponse { get; private set; }
        public string RerunStatus { get; private set; }
        public string RerunStatusReason { get; private set; }
        public List<ContentDeliveryRecordResponse> ContentDeliveryRecordsResponse { get; private set; }

        public InfoModel(ILogger<IndexModel> logger, IBackupRecordRepository backupRecordPersistanceService, IResourceGroupRepository resourceGroupPersistanceService)
        {
            this._logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                BackupRecordResponse = await _backupRecordPersistanceService.GetByIdAsync(id);
                if (record == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync("1");

                var url = $"api/BackupRecords/{id}";
                BackupRecordResponse = await _httpService.GetAsync<BackupRecordResponse>(url);
                if (BackupRecordResponse == null)
                    return RedirectToPage("Index");
                await GetContentDeliveryRecordsAsync(id);
                if (Request.Query.ContainsKey("re-run"))
                {
                    this.RerunStatus = Request.Query["re-run"];
                    if (Request.Query.ContainsKey("reason"))
                        this.RerunStatusReason = Request.Query["reason"];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToPage("Index");
            }
            return Page();
        }

        private async Task GetContentDeliveryRecordsAsync(string id)
        {
            try
            {
                var url = $"api/ContentDeliveryRecords/ByBackupRecordId/{id}";
                ContentDeliveryRecordsResponse = await _httpService.GetAsync<List<ContentDeliveryRecordResponse>>(url);
            }
            catch (Exception ex) { this._logger.LogWarning(ex.Message); }
        }
    }
}
