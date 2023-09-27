using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.DatabaseBackups
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;
        private readonly IResourceGroupRepository _resourceGroupPersistanceService;

        public List<BackupRecord> BackupRecordsResponse { get; set; }
        public IndexModel(ILogger<IndexModel> logger, IBackupRecordRepository backupRecordPersistanceService, IResourceGroupRepository resourceGroupPersistanceService)
        {
            this._logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                BackupRecordsResponse = await _backupRecordPersistanceService.GetAllAsync(Common.Shared.CurrentResourceGroup?.Id);
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(Common.Shared.CurrentResourceGroup?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Page();
        }
    }
}
