using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.ManagedDirectories
{
    public class CreateDirectoryModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDirectoryStorageService _directoryStorageService;

        [BindProperty]
        public ActiveDirectoryRequest ActiveDirectoryRequest { get; set; }
        public CreateDirectoryModel(ILogger<IndexModel> logger, IDirectoryStorageService directoryStorageService)
        {
            this._logger = logger;
            this._directoryStorageService = directoryStorageService;
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ActiveDirectoryRequest == null)
                    return Page();
                //Checks Pattern
                bool addedSuccess = await this._directoryStorageService.AddAsync(ActiveDirectoryRequest);
                if (addedSuccess)
                    return Redirect("/managed-directories/");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Page();
            }

        }
    }
}
