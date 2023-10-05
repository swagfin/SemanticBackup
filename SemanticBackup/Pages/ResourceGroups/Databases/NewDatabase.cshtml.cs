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
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;
        private readonly IBackupProviderForMySQLServer _backupProviderForMySQLServer;
        private readonly IBackupProviderForSQLServer _backupProviderForSQLServer;

        [BindProperty]
        public DatabaseInfoRequest backupDatabaseRequest { get; set; }
        [BindProperty]
        public IEnumerable<string> DatabaseNames { get; set; }
        public string ErrorResponse { get; set; } = null;
        public ResourceGroup CurrentResourceGroup { get; private set; }
        public List<string> AvailableDatabases { get; private set; } = new List<string>();

        public NewDatabaseModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoRepository, IBackupScheduleRepository backupScheduleRepository, IBackupProviderForMySQLServer backupProviderForMySQLServer, IBackupProviderForSQLServer backupProviderForSQLServer)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoRepository = databaseInfoRepository;
            this._backupScheduleRepository = backupScheduleRepository;
            this._backupProviderForMySQLServer = backupProviderForMySQLServer;
            this._backupProviderForSQLServer = backupProviderForSQLServer;
        }
        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                AvailableDatabases = await GetAvailableDatabaseCollectionsAsync();
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
                AvailableDatabases = await GetAvailableDatabaseCollectionsAsync();
                //proceed
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
                        DatabaseName = database.Trim(),
                        Description = backupDatabaseRequest.Description,
                        DateRegisteredUTC = DateTime.UtcNow
                    };
                    bool savedSuccess = await _databaseInfoRepository.AddOrUpdateAsync(saveObj);
                    if (!savedSuccess)
                        throw new Exception($"unable to save database: {saveObj.DatabaseName}");
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

        private async Task<List<string>> GetAvailableDatabaseCollectionsAsync()
        {
            List<string> dbCollections = new List<string>();
            try
            {
                if (CurrentResourceGroup.DbType.Contains("SQLSERVER"))
                {
                    dbCollections = await _backupProviderForSQLServer.GetAvailableDatabaseCollectionAsync(CurrentResourceGroup);
                }
                else if (CurrentResourceGroup.DbType.Contains("MYSQL") || CurrentResourceGroup.DbType.Contains("MARIADB"))
                {
                    dbCollections = await _backupProviderForMySQLServer.GetAvailableDatabaseCollectionAsync(CurrentResourceGroup);
                }
                else
                    throw new Exception("unsupported");
                //filter out
                List<string> alreadyAddedDbNames = await _databaseInfoRepository.GetDatabaseNamesForResourceGroupAsync(CurrentResourceGroup.Id);
                return dbCollections.Where(x => !alreadyAddedDbNames.Contains(x)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return dbCollections;
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
                    ScheduleType = BackupScheduleType.FULLBACKUP.ToString(),
                    EveryHours = 24,
                    StartDateUTC = new DateTime(currentTimeUTC.Year, currentTimeUTC.Month, currentTimeUTC.Day + 1),
                    CreatedOnUTC = currentTimeUTC,
                    Name = databaseInfo.DatabaseName
                };
                bool savedSuccess = await _backupScheduleRepository.AddOrUpdateAsync(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Auto Create Daily Backup for Database Key: {databaseInfo.Id},Error: {ex.Message}"); }
        }
    }
}
