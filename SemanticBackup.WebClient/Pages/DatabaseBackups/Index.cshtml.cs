using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.DatabaseBackups
{
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; }

        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;
        public List<BackupRecordResponse> BackupRecordsResponse { get; set; }
        public IndexModel(IOptions<WebClientOptions> options, IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = options.Value.WebApiUrl;
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
