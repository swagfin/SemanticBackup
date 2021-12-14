using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class ContentDeliveryRecordsController : ControllerBase
    {
        private readonly ILogger<ContentDeliveryRecordsController> _logger;
        private readonly IContentDeliveryRecordPersistanceService _contentDeliveryRecordPersistanceService;

        public ContentDeliveryRecordsController(ILogger<ContentDeliveryRecordsController> logger, IContentDeliveryRecordPersistanceService contentDeliveryRecordPersistanceService)
        {
            this._logger = logger;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
        }
        [HttpGet]
        public ActionResult<List<ContentDeliveryRecord>> Get(string resourcegroup)
        {
            try
            {
                return this._contentDeliveryRecordPersistanceService.GetAll(resourcegroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ContentDeliveryRecord>();
            }
        }

        [HttpGet("ByBackupRecordId/{id}")]
        public ActionResult<List<ContentDeliveryRecord>> GeByBackupRecordId(string id)
        {
            try
            {
                return this._contentDeliveryRecordPersistanceService.GetAllByBackupRecordId(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ContentDeliveryRecord>();
            }
        }

        [Route("re-run/{id}")]
        [HttpGet, HttpPost]
        public ActionResult GetInitRerun(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var contentDeliveryRecord = _contentDeliveryRecordPersistanceService.GetById(id);
                if (contentDeliveryRecord == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else if (contentDeliveryRecord.CurrentStatus != "ERROR")
                    return new ConflictObjectResult($"STATUS need to be ERROR, Current Status for this record is: {contentDeliveryRecord.CurrentStatus}");
                bool rerunSuccess = _contentDeliveryRecordPersistanceService.UpdateStatusFeed(id, ContentDeliveryRecordStatus.QUEUED.ToString(), "Queued for Re-run", 0);
                if (rerunSuccess)
                    return Ok(rerunSuccess);
                else
                    throw new Exception("Re-run was not initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
