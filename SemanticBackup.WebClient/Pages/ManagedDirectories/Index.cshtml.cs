using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.WebClient.Pages.ManagedDirectories
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDirectoryStorageService _directoryStorageService;
        public ActiveDirectory CurrentDirectory { get; private set; }
        public List<ActiveDirectory> OtherDirectories { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, IDirectoryStorageService directoryStorageService)
        {
            this._logger = logger;
            this._directoryStorageService = directoryStorageService;
        }

        public void OnGet()
        {
            try
            {
                CurrentDirectory = Directories.CurrentDirectory;
                var allDirectories = this._directoryStorageService.GetActiveDirectories();
                if (allDirectories != null && CurrentDirectory != null)
                    this.OtherDirectories = allDirectories.Where(x => x.Id != CurrentDirectory.Id).ToList();
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
        }
    }
}
