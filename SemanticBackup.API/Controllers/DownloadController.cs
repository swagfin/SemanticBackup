using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.IO;

namespace SemanticBackup.API.Controllers
{
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
        public ActionResult Get(string id, string token = null)
        {
            try
            {
                string _key = string.IsNullOrWhiteSpace(token) ? id : $"{id}?&token={token}";
                ContentDeliveryRecord contentDeliveryRecord = _contentDeliveryRecordPersistanceService.GetByContentTypeByExecutionMessage(ContentDeliveryType.DOWNLOAD_LINK.ToString(), _key);
                if (contentDeliveryRecord == null)
                    return new NotFoundObjectResult("No Download File with the Link Provided");
                BackupRecord backupRecord = _backupRecordPersistanceService.GetById(contentDeliveryRecord.BackupRecordId);
                if (backupRecord == null)
                    return new NotFoundObjectResult($"No Backup Record Information associated with the Link Provided: {id}");
                if (!System.IO.File.Exists(backupRecord.Path))
                    return new NotFoundObjectResult($"No Backup Record File associated with the Link Provided: {id}");
                return FileDownloadResponse(backupRecord.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ObjectResult($"Ooops!, Something went Wrong: {ex.Message}");
            }
        }

        private FileContentResult FileDownloadResponse(string fullFilePath)
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
            byte[] filedata = System.IO.File.ReadAllBytes(fullFilePath);
            return File(filedata, contentType);
        }
    }
}
