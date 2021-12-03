using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.DatabaseBackupSchedules
{
    public class RegisterScheduleModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<RegisterScheduleModel> _logger;

        [BindProperty]
        public BackupScheduleRequest backupScheduleRequest { get; set; }
        public List<BackupDatabaseInfoResponse> databaseInfoSelectList { get; set; }
        public RegisterScheduleModel(IHttpService httpService, ILogger<RegisterScheduleModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            databaseInfoSelectList = await GetBackupDatabaseInfoAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                backupScheduleRequest.BackupDatabaseInfoId = Guid.NewGuid().ToString();
                var url = "api/BackupSchedules/";
                var result = await _httpService.PostAsync<StatusResponseModel>(url, backupScheduleRequest);

            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex, ex.Message);
                throw;
            }
            return RedirectToPage("Index");
        }

        private async Task<List<BackupDatabaseInfoResponse>> GetBackupDatabaseInfoAsync()
        {
            var url = "api/BackupDatabases/";
            var DatabaseResponse = await _httpService.GetAsync<List<BackupDatabaseInfoResponse>>(url);
            return DatabaseResponse;
        }
    }
}
