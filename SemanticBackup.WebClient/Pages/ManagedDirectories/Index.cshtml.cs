using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.ManagedDirectories
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDirectoryStorageService _directoryStorageService;
        public string ApiEndPoint { get; }
        public ActiveDirectoryResponse CurrentDirectory { get; private set; }
        public List<ActiveDirectoryResponse> OtherDirectories { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, IDirectoryStorageService directoryStorageService, IOptions<WebClientOptions> options)
        {
            this._logger = logger;
            this._directoryStorageService = directoryStorageService;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var allDirectories = await this._directoryStorageService.GetAllAsync();
                CurrentDirectory = Directories.CurrentDirectory;
                if (allDirectories != null && CurrentDirectory != null)
                    this.OtherDirectories = allDirectories.Where(x => x.Id != CurrentDirectory.Id).ToList();
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
            return Page();
        }
    }
}
