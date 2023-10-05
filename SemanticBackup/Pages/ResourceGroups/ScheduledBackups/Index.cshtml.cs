using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.ScheduledBackups
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;

        public List<BackupSchedule> BackupSchedulesResponse { get; set; }
        public ResourceGroup CurrentResourceGroup { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IBackupScheduleRepository backupScheduleRepository)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._backupScheduleRepository = backupScheduleRepository;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                BackupSchedulesResponse = await _backupScheduleRepository.GetAllAsync(CurrentResourceGroup.Id);
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
