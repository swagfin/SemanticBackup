using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.BackupSchedules
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public List<BackupScheduleResponse> BackupSchedulesResponse { get; set; }
        public IndexModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var url = "api/BackupSchedules/";
                BackupSchedulesResponse = await _httpService.GetAsync<List<BackupScheduleResponse>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }
    }
}
