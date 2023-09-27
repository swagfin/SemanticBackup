using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.BackupSchedules
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public List<BackupSchedule> BackupSchedulesResponse { get; set; }
        public IndexModel(ILogger<IndexModel> logger)
        {
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                //var url = "api/BackupSchedules/";
                //BackupSchedulesResponse = await _httpService.GetAsync<List<BackupScheduleResponse>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }
    }
}
