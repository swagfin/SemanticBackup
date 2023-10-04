using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.ScheduledBackups
{
    [Authorize]
    public class NewScheduleModel : PageModel
    {
        private readonly ILogger<NewScheduleModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;

        [BindProperty]
        public BackupScheduleRequest BackupScheduleRequest { get; set; }
        public List<BackupDatabaseInfo> DatabaseInfoSelectList { get; set; }
        public string ErrorResponse { get; set; } = null;
        public ResourceGroup CurrentResourceGroup { get; private set; }

        public NewScheduleModel(ILogger<NewScheduleModel> logger, IResourceGroupRepository resourceGroupRepository, IBackupScheduleRepository backupScheduleRepository, IDatabaseInfoRepository databaseInfoRepository)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._backupScheduleRepository = backupScheduleRepository;
            this._databaseInfoRepository = databaseInfoRepository;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                await RefreshDbDropdownCollectionAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return Redirect("/");
            }
        }

        public async Task<IActionResult> OnPostAsync(string resourceGroupId)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);

                //proceed
                if (string.IsNullOrWhiteSpace(BackupScheduleRequest.BackupDatabaseInfoId))
                    ErrorResponse = "You have not Selected any Database from the List";
                else if (string.IsNullOrWhiteSpace(BackupScheduleRequest.ScheduleType))
                    ErrorResponse = "First select Database Backup Schedule Type";
                else
                {
                    //get database info
                    BackupDatabaseInfo validDatabaseInfo = await _databaseInfoRepository.VerifyDatabaseInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, BackupScheduleRequest.BackupDatabaseInfoId);
                    //Proceed
                    BackupSchedule saveObj = new BackupSchedule
                    {
                        BackupDatabaseInfoId = BackupScheduleRequest.BackupDatabaseInfoId,
                        ScheduleType = BackupScheduleRequest.ScheduleType,
                        EveryHours = BackupScheduleRequest.EveryHours,
                        StartDateUTC = BackupScheduleRequest.StartDate,
                        CreatedOnUTC = DateTime.UtcNow,
                        Name = validDatabaseInfo.Name,
                    };
                    bool savedSuccess = await _backupScheduleRepository.AddOrUpdateAsync(saveObj);
                    if (!savedSuccess)
                        throw new Exception("Data was not Saved");
                    //redirect
                    return Redirect($"/resource-groups/{resourceGroupId}/scheduled-backups");
                }
            }
            catch (Exception ex)
            {
                ErrorResponse = ex.Message;
                _logger.LogInformation(ex.Message);
            }
            await RefreshDbDropdownCollectionAsync();
            return Page();
        }

        private async Task RefreshDbDropdownCollectionAsync()
        {
            try
            {
                DatabaseInfoSelectList = await _databaseInfoRepository.GetAllAsync(CurrentResourceGroup.Id);
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
        }
    }
}
