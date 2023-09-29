using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.Databases
{
    [Authorize]
    public class NewDatabaseModel : PageModel
    {
        public string AuthToken { get; }

        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;

        public string ApiEndPoint { get; }
        [BindProperty]
        public DatabaseInfoRequest backupDatabaseRequest { get; set; }
        [BindProperty]
        public IEnumerable<string> DatabaseNames { get; set; }
        public string ErrorResponse { get; set; } = null;
        public ResourceGroup CurrentResourceGroup { get; private set; }

        public NewDatabaseModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoRepository, IBackupScheduleRepository backupScheduleRepository)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoRepository = databaseInfoRepository;
            this._backupScheduleRepository = backupScheduleRepository;
        }
        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
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
                ErrorResponse = null;
                //re-attemp to get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                //proceed
                if (string.IsNullOrWhiteSpace(backupDatabaseRequest.DatabaseType))
                {
                    ErrorResponse = "First Select the Database Type";
                    return Page();
                }
                if (string.IsNullOrWhiteSpace(backupDatabaseRequest.Server))
                {
                    ErrorResponse = "Server Name was not provided";
                    return Page();
                }
                if (DatabaseNames == null || DatabaseNames.Count() < 1)
                {
                    ErrorResponse = "Select or add atlist one Database";
                    return Page();
                }

                foreach (string database in DatabaseNames.ToList())
                {
                    BackupDatabaseInfo saveObj = new BackupDatabaseInfo
                    {
                        ResourceGroupId = CurrentResourceGroup.Id,
                        Server = backupDatabaseRequest.Server,
                        DatabaseName = database.Trim(),
                        Username = backupDatabaseRequest.Username,
                        Password = backupDatabaseRequest.Password,
                        DatabaseType = backupDatabaseRequest.DatabaseType,
                        Port = backupDatabaseRequest.Port,
                        Description = backupDatabaseRequest.Description,
                        DateRegisteredUTC = DateTime.UtcNow
                    };
                    bool savedSuccess = await _databaseInfoRepository.AddOrUpdateAsync(saveObj);
                    if (!savedSuccess)
                        throw new Exception($"unable to save database: {saveObj.Name}");
                    if (backupDatabaseRequest.AutoCreateSchedule)
                        await CreateScheduleForAsync(saveObj);
                }
                //redirect to databases
                return Redirect($"/resource-groups/{resourceGroupId}/databases");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorResponse = ex.Message;
                return Page();
            }
        }

        private async Task CreateScheduleForAsync(BackupDatabaseInfo databaseInfo)
        {
            try
            {
                DateTime currentTimeUTC = DateTime.UtcNow;
                BackupSchedule saveObj = new BackupSchedule
                {
                    BackupDatabaseInfoId = databaseInfo.Id,
                    ResourceGroupId = databaseInfo.ResourceGroupId,
                    ScheduleType = BackupScheduleType.FULLBACKUP.ToString(),
                    EveryHours = 24,
                    StartDateUTC = new DateTime(currentTimeUTC.Year, currentTimeUTC.Month, currentTimeUTC.Day + 1),
                    CreatedOnUTC = currentTimeUTC,
                    Name = databaseInfo.Name
                };
                bool savedSuccess = await _backupScheduleRepository.AddOrUpdateAsync(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Auto Create Daily Backup for Database Key: {databaseInfo.Id},Error: {ex.Message}"); }
        }
    }
}
