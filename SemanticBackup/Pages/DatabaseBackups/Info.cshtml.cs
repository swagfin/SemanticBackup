using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.Models.Response;
using SemanticBackup.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.DatabaseBackups
{
    [Authorize]
    public class InfoModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public string ApiEndPoint { get; }
        public BackupRecordResponse BackupRecordResponse { get; private set; }
        public string RerunStatus { get; private set; }
        public string RerunStatusReason { get; private set; }
        public List<ContentDeliveryRecordResponse> ContentDeliveryRecordsResponse { get; private set; }

        public InfoModel(IHttpService httpService, ILogger<IndexModel> logger, IOptions<WebClientOptions> options)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
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
