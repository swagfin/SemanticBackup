using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.Databases
{
    public class IndexModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public List<BackupDatabaseInfoResponse> DatabaseResponse { get; set; }
        public IndexModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var url = "api/BackupDatabases/";
                DatabaseResponse = await _httpService.GetAsync<List<BackupDatabaseInfoResponse>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }
    }
}
