using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Services;
using System;

namespace SemanticBackup.WebClient.Pages.ManagedDirectories
{
    public class CreateDirectoryModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDirectoryStorageService _directoryStorageService;

        [BindProperty]
        public ActiveDirectory ActiveDirectoryRequest { get; set; }
        public CreateDirectoryModel(ILogger<IndexModel> logger, IDirectoryStorageService directoryStorageService)
        {
            this._logger = logger;
            this._directoryStorageService = directoryStorageService;
        }
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            try
            {
                if (ActiveDirectoryRequest == null)
                    return Page();
                //Proceed
                bool addedSuccess = this._directoryStorageService.AddDirectory(ActiveDirectoryRequest);
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
