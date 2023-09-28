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
    public class DetailsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public string ApiEndPoint { get; }
        public BackupDatabaseInfo DatabaseResponse { get; set; }
        public List<BackupRecord> BackupRecordsResponse { get; private set; }
        public List<BackupSchedule> BackupSchedulesResponse { get; private set; }

        public DetailsModel(ILogger<IndexModel> logger, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            this._logger = logger;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId, string id)
        {
            try
            {
                var url = $"api/BackupDatabases/{id}";
                DatabaseResponse = await _databaseInfoPersistanceService.GetByIdAsync(id);
                //Get Backups
                await GetBackupRecordsForDatabaseAsync(id);
                await GetBackupSchedulesForDatabaseAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToPage("Index");
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                //    if (string.IsNullOrWhiteSpace(id))
                //        throw new Exception("Id can't be NULL");
                //    var backupDatabaseInfo = await _databaseInfoPersistanceService.GetByIdAsync(id);
                //    if (backupDatabaseInfo == null)
                //        return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //    //Check if an Existing Queued
                //    var queuedExisting = await this._backupRecordPersistanceService.GetAllByDatabaseIdByStatusAsync(backupDatabaseInfo.ResourceGroupId, backupDatabaseInfo.Id, BackupRecordBackupStatus.QUEUED.ToString());
                //    if (queuedExisting != null && queuedExisting.Count > 0)
                //    {
                //        //No Need to Create another Just Return
                //        return queuedExisting.FirstOrDefault();
                //    }
                //    //Resource Group
                //    ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo?.ResourceGroupId);
                //    //Proceed Otherwise
                //    DateTime currentTimeUTC = DateTime.UtcNow;
                //    DateTime currentTimeLocal = currentTimeUTC.ConvertFromUTC(resourceGroup?.TimeZone);
                //    DateTime RecordExpiryUTC = currentTimeUTC.AddDays(resourceGroup.BackupExpiryAgeInDays);
                //    BackupRecord newRecord = new BackupRecord
                //    {
                //        BackupDatabaseInfoId = backupDatabaseInfo.Id,
                //        ResourceGroupId = backupDatabaseInfo.ResourceGroupId,
                //        BackupStatus = BackupRecordBackupStatus.QUEUED.ToString(),
                //        ExpiryDateUTC = RecordExpiryUTC,
                //        Name = backupDatabaseInfo.Name,
                //        Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, SharedFunctions.GetSavingPathFromFormat(backupDatabaseInfo, _persistanceOptions.BackupFileSaveFormat, currentTimeLocal)),
                //        StatusUpdateDateUTC = currentTimeUTC,
                //        RegisteredDateUTC = currentTimeUTC,
                //        ExecutedDeliveryRun = false
                //    };
                //    bool addedSuccess = await this._backupRecordPersistanceService.AddOrUpdateAsync(newRecord);
                //    return Redirect($"/databasebackups/{backupRecord.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Redirect($"/databases/{id}");
        }

        private async Task GetBackupRecordsForDatabaseAsync(string id)
        {
            try
            {
                //var url = $"api/BackupRecords/ByDatabaseId/{id}";
                //var records = await _httpService.GetAsync<List<BackupRecordResponse>>(url);
                //if (records != null)
                //    BackupRecordsResponse = records.Take(10).ToList();
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Backup Records for Db: {id}, Error: {ex.Message}"); }

        }
        private async Task GetBackupSchedulesForDatabaseAsync(string id)
        {
            try
            {
                //var url = $"api/BackupSchedules/ByDatabaseId/{id}";
                //BackupSchedulesResponse = await _httpService.GetAsync<List<BackupScheduleResponse>>(url);
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Schedules for Db: {id}, Error: {ex.Message}"); }
        }
    }
}
