using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.DatabaseBackups
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;

        public ResourceGroup CurrentResourceGroup { get; private set; }
        public List<BackupRecord> BackupRecordsResponse { get; set; }
        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IBackupRecordRepository backupRecordPersistanceService)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                BackupRecordsResponse = await _backupRecordPersistanceService.GetAllAsync(CurrentResourceGroup.Id);
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
