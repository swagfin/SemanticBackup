using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.Databases
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IBackupScheduleRepository _backupScheduleRepository;
        private readonly SystemConfigOptions _options;

        public ResourceGroup CurrentResourceGroup { get; private set; }
        public BackupDatabaseInfo DatabaseInfoResponse { get; set; }
        public List<BackupRecord> BackupRecordsResponse { get; private set; }
        public List<BackupSchedule> BackupSchedulesResponse { get; private set; }

        public DetailsModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoPersistanceService, IBackupRecordRepository backupRecordRepository, IBackupScheduleRepository backupScheduleRepository, SystemConfigOptions options)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._databaseInfoRepository = databaseInfoPersistanceService;
            this._backupRecordRepository = backupRecordRepository;
            this._backupScheduleRepository = backupScheduleRepository;
            this._options = options;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId, string id)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                DatabaseInfoResponse = await _databaseInfoRepository.VerifyDatabaseInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, id);
                //get instant backups
                if (Request.Query.ContainsKey("request-backup"))
                {
                    long? backupKeyId = await InitiateDatabaseBackupAsync();
                    if (backupKeyId != null && backupKeyId != 0)
                        return Redirect($"/resource-groups/{resourceGroupId}/database-backups/details/{backupKeyId}");
                    return Redirect($"/resource-groups/{resourceGroupId}/databases/details/{id}/?response=backup-request-failed");
                }
                //reload All
                await GetBackupRecordsForDatabaseAsync(DatabaseInfoResponse.Id);
                await GetBackupSchedulesForDatabaseAsync(DatabaseInfoResponse.Id);

                //#Check Other Requests
                if (Request.Query.ContainsKey("remove-backup-schedule"))
                {
                    //user attempting to remove
                    BackupSchedule backupSchedule = BackupSchedulesResponse.FirstOrDefault(x => x.Id.Equals(Request.Query["remove-backup-schedule"].ToString()?.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (backupSchedule == null)
                        return Redirect($"/resource-groups/{resourceGroupId}/databases/details/{id}/?schedules=active&response=unknown-backup-schedule");
                    //proceed
                    string scheduledRemoved = (await _backupScheduleRepository.RemoveAsync(backupSchedule.Id)) ? "success" : "failed";
                    return Redirect($"/resource-groups/{resourceGroupId}/databases/details/{id}/?schedules=active&response={scheduledRemoved}");
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect("/");
            }
        }

        private async Task<long?> InitiateDatabaseBackupAsync()
        {
            try
            {
                if (DatabaseInfoResponse == null)
                    return null;
                //Check if an Existing Queued
                List<BackupRecord> queuedExisting = await this._backupRecordRepository.GetAllByDatabaseIdByStatusAsync(DatabaseInfoResponse.Id, BackupRecordStatus.QUEUED.ToString());
                if (queuedExisting != null && queuedExisting.Count > 0)
                    return queuedExisting.FirstOrDefault()?.Id;
                //init requeue db
                DateTime currentTimeUTC = DateTime.UtcNow;
                DateTime currentTimeLocal = DateTime.Now;
                DateTime RecordExpiryUTC = currentTimeUTC.AddDays(CurrentResourceGroup.BackupExpiryAgeInDays);
                BackupRecord newRecord = new BackupRecord
                {
                    BackupDatabaseInfoId = DatabaseInfoResponse.Id,
                    BackupStatus = BackupRecordStatus.QUEUED.ToString(),
                    ExpiryDateUTC = RecordExpiryUTC,
                    Name = $"{DatabaseInfoResponse.DatabaseName} on {CurrentResourceGroup.DbServer}",
                    Path = Path.Combine(_options.DefaultBackupDirectory, CurrentResourceGroup.GetSavingPathFromFormat(DatabaseInfoResponse.DatabaseName, _options.BackupFileSaveFormat, currentTimeUTC)),
                    StatusUpdateDateUTC = currentTimeUTC,
                    RegisteredDateUTC = currentTimeUTC,
                    ExecutedDeliveryRun = false
                };
                bool addedSuccess = await this._backupRecordRepository.AddOrUpdateAsync(newRecord);
                return addedSuccess ? newRecord.Id : throw new Exception("could not save queue for a instant backup");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unable to request for backup for database: {DatabaseInfoResponse.Id}, Error: {ex.Message}");
                return null;
            }
        }


        private async Task GetBackupRecordsForDatabaseAsync(string id)
        {
            try
            {
                BackupRecordsResponse = (await _backupRecordRepository.GetAllByDatabaseIdAsync(id))?.Take(10).ToList();
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Backup Records for Db: {id}, Error: {ex.Message}"); }

        }
        private async Task GetBackupSchedulesForDatabaseAsync(string id)
        {
            try
            {
                BackupSchedulesResponse = await _backupScheduleRepository.GetAllByDatabaseIdAsync(id);
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Schedules for Db: {id}, Error: {ex.Message}"); }
        }
    }
}
