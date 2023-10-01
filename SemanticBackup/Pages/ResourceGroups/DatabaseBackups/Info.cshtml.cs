using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.DatabaseBackups
{
    [Authorize]
    public class InfoModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _contentDeliveryRecordRepository;

        public BackupRecord BackupRecordResponse { get; private set; }
        public string RerunStatus { get; private set; }
        public string RerunStatusReason { get; private set; }
        public List<ContentDeliveryRecord> ContentDeliveryRecordsResponse { get; private set; }
        public ResourceGroup CurrentResourceGroup { get; private set; }

        public InfoModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IBackupRecordRepository backupRecordRepository, IContentDeliveryRecordRepository contentDeliveryRecordRepository)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._backupRecordRepository = backupRecordRepository;
            this._contentDeliveryRecordRepository = contentDeliveryRecordRepository;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId, string id)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                //get backup record under resource group
                BackupRecordResponse = await _backupRecordRepository.VerifyBackupRecordInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, id);
                //get content delivery records
                await GetContentDeliveryRecordsAsync(BackupRecordResponse.Id);
                //reset re-runs params
                this.RerunStatus = null;
                this.RerunStatusReason = null;
                //check if re-run selection
                if (Request.Query.ContainsKey("re-run") && Request.Query.ContainsKey("job"))
                    switch (Request.Query["re-run"].ToString()?.Trim().ToLower())
                    {
                        case "backup":
                            //rerun backup
                            await InitiateRerunForBackupAsync(BackupRecordResponse.Id);
                            break;
                        case "delivery":
                            //rerun delivery
                            await InitiateRerunForDeliveryAsync(Request.Query["job"].ToString()?.Trim().ToLower());
                            break;
                    }
                //return page
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/resource-groups/{id}/database-backups");
            }
        }
        private async Task GetContentDeliveryRecordsAsync(string backupRecordId)
        {
            try
            {
                ContentDeliveryRecordsResponse = await _contentDeliveryRecordRepository.GetAllByBackupRecordIdAsync(backupRecordId);
            }
            catch (Exception ex) { this._logger.LogWarning(ex.Message); }
        }

        private async Task InitiateRerunForBackupAsync(string rerunJobId)
        {
            try
            {
                //re-run here
                //:: ensure backup record exists
                if (BackupRecordResponse.BackupStatus != "ERROR")
                    throw new Exception($"STATUS need to be ERROR, Current Status for this record is: {BackupRecordResponse.BackupStatus}");
                //prepare re-run
                string newBackupPath = BackupRecordResponse.Path.Replace(".zip", ".bak");
                bool rerunSuccess = await _backupRecordRepository.UpdateStatusFeedAsync(BackupRecordResponse.Id, BackupRecordBackupStatus.QUEUED.ToString(), "Queued for Re-run", 0, newBackupPath);
                //update status
                this.RerunStatus = "success";
                this.RerunStatusReason = "Success";
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex.Message);
                this.RerunStatus = "failed";
                this.RerunStatusReason = ex.Message;
            }
        }
        private async Task InitiateRerunForDeliveryAsync(string rerunJobId)
        {
            try
            {
                //re-run here
                ContentDeliveryRecord contentDeliveryRecord = ContentDeliveryRecordsResponse.FirstOrDefault(x => x.Id == rerunJobId);
                if (contentDeliveryRecord == null)
                    throw new Exception($"No delivery content with specified job: {rerunJobId}");
                //check status
                else if (contentDeliveryRecord.CurrentStatus != "ERROR")
                    throw new Exception($"STATUS needs to be ERROR state, Current Status for this record is: {contentDeliveryRecord.CurrentStatus}");
                //prepare re-run
                bool rerunSuccess = await _contentDeliveryRecordRepository.UpdateStatusFeedAsync(contentDeliveryRecord.Id, ContentDeliveryRecordStatus.QUEUED.ToString(), "Queued for Re-run", 0);
                this.RerunStatus = "success";
                this.RerunStatusReason = "Success";
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex.Message);
                this.RerunStatus = "failed";
                this.RerunStatusReason = ex.Message;
            }
        }
    }
}