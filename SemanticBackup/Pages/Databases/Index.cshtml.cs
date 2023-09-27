using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.Databases
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public List<BackupDatabaseInfo> DatabaseResponse { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            this._logger = logger;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DatabaseResponse = await _databaseInfoPersistanceService.GetAllAsync("1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }

    }
}
