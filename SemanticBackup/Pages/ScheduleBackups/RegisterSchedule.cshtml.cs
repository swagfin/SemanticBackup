using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.DatabaseBackupSchedules
{
    [Authorize]
    public class RegisterScheduleModel : PageModel
    {
        private readonly ILogger<RegisterScheduleModel> _logger;

        [BindProperty]
        public BackupScheduleRequest backupScheduleRequest { get; set; }
        public List<BackupDatabaseInfo> databaseInfoSelectList { get; set; }
        public RegisterScheduleModel(ILogger<RegisterScheduleModel> logger)
        {
            this._logger = logger;
        }
        public string ErrorResponse { get; set; } = null;
        public async Task<IActionResult> OnGetAsync()
        {
            await RefreshDbDropdownCollectionAsync();
            return Page();
        }

        private async Task RefreshDbDropdownCollectionAsync()
        {
            try
            {
                databaseInfoSelectList = await GetBackupDatabaseInfoAsync();
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupScheduleRequest.BackupDatabaseInfoId))
                    ErrorResponse = "You have not Selected any Database from the List";
                else if (string.IsNullOrWhiteSpace(backupScheduleRequest.ScheduleType))
                    ErrorResponse = "First select Database Backup Schedule Type";
                else
                {
                    ////Proceed
                    //var url = "api/BackupSchedules/";
                    //var result = await _httpService.PostAsync<StatusResponseModel>(url, backupScheduleRequest);
                    return RedirectToPage("Index");
                }
            }
            catch (System.Exception ex)
            {
                ErrorResponse = ex.Message;
                _logger.LogInformation(ex.Message);
            }
            await RefreshDbDropdownCollectionAsync();
            return Page();
        }

        private async Task<List<BackupDatabaseInfo>> GetBackupDatabaseInfoAsync()
        {
            //var url = "api/BackupDatabases/";
            //var DatabaseResponse = await _httpService.GetAsync<List<BackupDatabaseInfoResponse>>(url);
            //return DatabaseResponse;
            await Task.Delay(1);
            return null;
        }
    }
}
