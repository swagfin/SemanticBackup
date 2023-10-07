using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.Databases
{
    public class RemoveModel : PageModel
    {
        private readonly ILogger<RemoveModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        public ResourceGroup CurrentResourceGroup { get; private set; }
        public BackupDatabaseInfo CurrentRecord { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public RemoveModel(ILogger<RemoveModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoRepository = databaseInfoPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId, string id)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                CurrentRecord = await _databaseInfoRepository.VerifyDatabaseInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, id);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect("/");
            }
        }

        public async Task<IActionResult> OnPostAsync(string resourceGroupId, string id)
        {
            try
            {
                ErrorMessage = null;
                //re-validate details
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                CurrentRecord = await _databaseInfoRepository.VerifyDatabaseInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, id);
                //Confirm Delete
                bool success = await this._databaseInfoRepository.RemoveAsync(CurrentRecord.Id);
                if (!success) throw new Exception("database info was not deleted");
                return Redirect($"/resource-groups/{resourceGroupId}/databases");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
                return Page();
            }
        }
    }
}