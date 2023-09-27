using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.DatabaseBackups
{
    [Authorize]
    public class RerunModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public RerunModel(ILogger<IndexModel> logger)
        {
            this._logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string id, string id2, string type = null)
        {
            bool isRecordRerun = (string.IsNullOrEmpty(type) || type == "record");
            if (isRecordRerun)
                return await RerunRecordAsync(id, id2);
            else
                return await RerunRecordContentDeliveryAsync(id, id2);
        }
        private async Task<IActionResult> RerunRecordAsync(string id, string id2)
        {
            try
            {
                //Proceeed 
                var rerunUrl = $"api/BackupRecords/re-run/{id}";
                //var rerunSuccess = await _httpService.GetAsync<bool>(rerunUrl);
                return Redirect($"/databasebackups/{id2}/?re-run=success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/databasebackups/{id2}/?re-run=failed&reason=exception");
            }
        }

        private async Task<IActionResult> RerunRecordContentDeliveryAsync(string id, string id2)
        {
            try
            {
                //Proceeed 

                var rerunUrl = $"api/ContentDeliveryRecords/re-run/{id}";
                //var rerunSuccess = await _httpService.GetAsync<bool>(rerunUrl);
                return Redirect($"/databasebackups/{id2}/?content-delivery-re-run=success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/databasebackups/{id2}/?content-delivery-re-run=failed&reason=exception");
            }
        }

    }
}
