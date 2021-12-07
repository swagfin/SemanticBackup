using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.DatabaseBackups
{
    public class InfoModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public string ApiEndPoint { get; }
        public BackupRecordResponse BackupRecordResponse { get; private set; }
        public string RerunStatus { get; private set; }
        public string RerunStatusReason { get; private set; }

        public InfoModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = Directories.CurrentDirectory?.Url;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                var url = $"api/BackupRecords/{id}";
                BackupRecordResponse = await _httpService.GetAsync<BackupRecordResponse>(url);
                if (BackupRecordResponse == null)
                    return RedirectToPage("Index");
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
    }
}
