using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.Databases
{
    public class InfoModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public string ApiEndPoint { get; }
        public BackupDatabaseInfoResponse DatabaseResponse { get; set; }
        public List<BackupRecordResponse> BackupRecordsResponse { get; private set; }
        public List<BackupScheduleResponse> BackupSchedulesResponse { get; private set; }

        public InfoModel(IHttpService httpService, ILogger<IndexModel> logger, IOptions<WebClientOptions> options)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                var url = $"api/BackupDatabases/{id}";
                DatabaseResponse = await _httpService.GetAsync<BackupDatabaseInfoResponse>(url);
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
                var url = $"api/BackupRecords/request-instant-backup/{id}";
                BackupRecordResponse backupRecord = await _httpService.GetAsync<BackupRecordResponse>(url);
                return Redirect($"/databasebackups/{backupRecord.Id}");
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
                var url = $"api/BackupRecords/ByDatabaseId/{id}";
                var records = await _httpService.GetAsync<List<BackupRecordResponse>>(url);
                if (records != null)
                    BackupRecordsResponse = records.Take(10).ToList();
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Backup Records for Db: {id}, Error: {ex.Message}"); }

        }
        private async Task GetBackupSchedulesForDatabaseAsync(string id)
        {
            try
            {
                var url = $"api/BackupSchedules/ByDatabaseId/{id}";
                BackupSchedulesResponse = await _httpService.GetAsync<List<BackupScheduleResponse>>(url);
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Get Database Schedules for Db: {id}, Error: {ex.Message}"); }
        }
    }
}
