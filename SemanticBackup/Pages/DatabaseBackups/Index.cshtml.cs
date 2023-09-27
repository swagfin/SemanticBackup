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
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; }

        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;
        public List<BackupRecordResponse> BackupRecordsResponse { get; set; }
        public IndexModel(IHttpService httpService, ILogger<IndexModel> logger, IOptions<WebClientOptions> options)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var url = "api/BackupRecords/";
                BackupRecordsResponse = await _httpService.GetAsync<List<BackupRecordResponse>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }
    }
}
