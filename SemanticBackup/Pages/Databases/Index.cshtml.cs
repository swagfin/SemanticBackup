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
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public ResourceGroup CurrentResourceGroup { get; private set; }
        public List<BackupDatabaseInfo> DatabaseResponse { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                DatabaseResponse = await _databaseInfoPersistanceService.GetAllAsync(resourceGroupId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect("/");
            }
        }

    }
}
