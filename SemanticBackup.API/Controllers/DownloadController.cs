using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [AllowAnonymous]
    [Route("/d/{id}")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly ILogger<DownloadController> _logger;
        private readonly IContentDeliveryRecordPersistanceService _contentDeliveryRecordPersistanceService;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;

        public DownloadController(ILogger<DownloadController> logger, IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService, IBackupRecordPersistanceService backupRecordPersistanceService)
        {
            this._logger = logger;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAsync(string id, string token = null)
        {
            try
            {
                string _key = string.IsNullOrWhiteSpace(token) ? id : $"{id}?token={token}";
                ContentDeliveryRecord contentDeliveryRecord = await _contentDeliveryRecordPersistanceService.GetByContentTypeByExecutionMessageAsync(ContentDeliveryType.DIRECT_LINK.ToString(), _key);
                if (contentDeliveryRecord == null)
                    return new NotFoundObjectResult("No Download File with the Link Provided");
                BackupRecord backupRecord = await _backupRecordPersistanceService.GetByIdAsync(contentDeliveryRecord.BackupRecordId);
                if (backupRecord == null)
                    return new NotFoundObjectResult($"No Backup Record Information associated with the Link Provided: {id}");
                if (!System.IO.File.Exists(backupRecord.Path))
                    return new NotFoundObjectResult($"No Backup Record File associated with the Link Provided: {id}");
                return await FileDownloadResponseAsync(backupRecord.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ObjectResult($"Ooops!, Something went Wrong: {ex.Message}");
            }
        }

        private async Task<FileContentResult> FileDownloadResponseAsync(string fullFilePath)
        {
            if (string.IsNullOrEmpty(fullFilePath))
                return null;
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(fullFilePath, out contentType);
            contentType = contentType ?? "application/octet-stream";
            string fileName = Path.GetFileName(fullFilePath);
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = true,
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            byte[] filedata = await System.IO.File.ReadAllBytesAsync(fullFilePath);
            return File(filedata, contentType);
        }
    }
}
