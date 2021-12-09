using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.ManagedDirectories
{
    public class SwitchModel : PageModel
    {
        private readonly ILogger<SwitchModel> _logger;
        private readonly IDirectoryStorageService _directoryStorageService;

        public SwitchModel(ILogger<SwitchModel> logger, IDirectoryStorageService directoryStorageService)
        {
            this._logger = logger;
            this._directoryStorageService = directoryStorageService;
        }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Redirect("/managed-directories/");
                bool switchedSuccess = await this._directoryStorageService.SwitchAsync(id);
                if (switchedSuccess)
                    return Redirect("/");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message); return Page();
            }
        }
    }
}
