using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.DatabaseBackups
{
    public class RerunModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;
        public BackupRecordResponse BackupRecordResponse { get; private set; }

        public RerunModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                var url = $"api/BackupRecords/{id}";
                BackupRecordResponse = await _httpService.GetAsync<BackupRecordResponse>(url);
                if (BackupRecordResponse == null)
                    return RedirectToPage("Index");
                if (BackupRecordResponse.BackupStatus != "ERROR")
                    return Redirect($"/databasebackups/{BackupRecordResponse.Id}/?re-run=failed&reason=backupstatus");
                //Proceeed 
                var rerunUrl = $"api/BackupRecords/re-run/{id}";
                var rerunSuccess = await _httpService.GetAsync<bool>(rerunUrl);
                return Redirect($"/databasebackups/{BackupRecordResponse.Id}/?re-run=success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/databasebackups/{BackupRecordResponse.Id}/?re-run=failed&reason=exception");
            }
        }
    }
}
