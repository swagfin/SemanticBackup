using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
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
    public class DetailsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IContentDeliveryRecordRepository _contentDeliveryRecordRepository;

        public BackupRecord BackupRecordResponse { get; private set; }
        public string RerunStatus { get; private set; }
        public string RerunStatusReason { get; private set; }
        public List<BackupRecordDelivery> ContentDeliveryRecordsResponse { get; private set; }
        public ResourceGroup CurrentResourceGroup { get; private set; }

        public DetailsModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IBackupRecordRepository backupRecordRepository, IContentDeliveryRecordRepository contentDeliveryRecordRepository)
        {
            this._logger = logger;
            this._resourceGroupRepository = resourceGroupRepository;
            this._backupRecordRepository = backupRecordRepository;
            this._contentDeliveryRecordRepository = contentDeliveryRecordRepository;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId, long id)
        {
            try
            {
                //get resource group
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                //get backup record under resource group
                BackupRecordResponse = await _backupRecordRepository.VerifyBackupRecordInResourceGroupThrowIfNotExistAsync(CurrentResourceGroup.Id, id);
                //get content delivery records
                await GetContentDeliveryRecordsAsync();
                //reset re-runs params
                this.RerunStatus = null;
                this.RerunStatusReason = null;
                //check if re-run selection
                if (Request.Query.ContainsKey("re-run") && Request.Query.ContainsKey("job"))
                    switch (Request.Query["re-run"].ToString()?.Trim().ToLower())
                    {
                        case "backup":
                            //rerun backup
                            await InitiateRerunForBackupAsync();
                            break;
                        case "delivery":
                            //rerun delivery
                            await InitiateRerunForDeliveryAsync(Request.Query["job"].ToString()?.Trim().ToLower());
                            break;
                    }
                else if (Request.Query.ContainsKey("download"))
                {
                    string contentKey = Request.Query["download"].ToString()?.Trim();
                    BackupRecordDelivery deliveryRecord = ContentDeliveryRecordsResponse.FirstOrDefault(x => x.DeliveryType == BackupDeliveryConfigTypes.DownloadLink.ToString() && x.ExecutionMessage == contentKey);
                    if (deliveryRecord == null)
                        return new NotFoundObjectResult($"no downloadable content with with specified ref: {contentKey}");
                    //return downloadable content
                    return await DownloadableContentAsync(deliveryRecord);
                }
                else if (Request.Query.ContainsKey("abandon-backup"))
                {
                    bool isSuccess = await _backupRecordRepository.UpdateExpiryDateByIdAsync(BackupRecordResponse.Id, DateTime.UtcNow.AddMinutes(-1));
                    if (isSuccess)
                        return Redirect($"/resource-groups/{resourceGroupId}/database-backups/details/{id}");
                }
                //return page
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return Redirect($"/resource-groups/{id}/database-backups");
            }
        }

        private async Task GetContentDeliveryRecordsAsync()
        {
            try
            {
                ContentDeliveryRecordsResponse = await _contentDeliveryRecordRepository.GetAllByBackupRecordIdAsync(BackupRecordResponse.Id);
            }
            catch (Exception ex) { this._logger.LogWarning(ex.Message); }
        }

        private async Task InitiateRerunForBackupAsync()
        {
            try
            {
                //re-run here
                //:: ensure backup record exists
                if (BackupRecordResponse.BackupStatus != BackupRecordStatus.ERROR.ToString())
                    throw new Exception($"STATUS need to be ERROR, Current Status for this record is: {BackupRecordResponse.BackupStatus}");
                //prepare re-run
                string newBackupPath = BackupRecordResponse.Path.Replace(".zip", ".bak");
                bool rerunSuccess = await _backupRecordRepository.UpdateStatusFeedAsync(BackupRecordResponse.Id, BackupRecordStatus.QUEUED.ToString(), "Queued for Re-run", 0, newBackupPath);
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
                BackupRecordDelivery contentDeliveryRecord = ContentDeliveryRecordsResponse.FirstOrDefault(x => x.Id.Equals(rerunJobId, StringComparison.CurrentCultureIgnoreCase));
                if (contentDeliveryRecord == null)
                    throw new Exception($"No delivery content with specified job: {rerunJobId}");
                //check status
                else if (contentDeliveryRecord.CurrentStatus != BackupRecordStatus.ERROR.ToString())
                    throw new Exception($"STATUS needs to be ERROR state, Current Status for this record is: {contentDeliveryRecord.CurrentStatus}");
                //prepare re-run
                bool rerunSuccess = await _contentDeliveryRecordRepository.UpdateStatusFeedAsync(contentDeliveryRecord.Id, BackupRecordDeliveryStatus.QUEUED.ToString(), "Queued for Re-run", 0);
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

        private async Task<IActionResult> DownloadableContentAsync(BackupRecordDelivery deliveryRecord)
        {
            if (!System.IO.File.Exists(BackupRecordResponse.Path))
                return new NotFoundObjectResult($"Physical Backup File appears to be missing");

            new FileExtensionContentTypeProvider().TryGetContentType(BackupRecordResponse.Path, out string contentType);
            contentType = contentType ?? "application/octet-stream";
            string fileName = System.IO.Path.GetFileName(BackupRecordResponse.Path);
            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = fileName,
                Inline = true,
            };
            Response.Headers.TryAdd("Content-Disposition", cd.ToString());
            byte[] filedata = await System.IO.File.ReadAllBytesAsync(BackupRecordResponse.Path);
            return File(filedata, contentType);
        }
    }
}
