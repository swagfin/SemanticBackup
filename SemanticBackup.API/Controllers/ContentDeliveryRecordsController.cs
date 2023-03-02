using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SemanticBackup.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class ContentDeliveryRecordsController : ControllerBase
    {
        private readonly ILogger<ContentDeliveryRecordsController> _logger;
        private readonly IContentDeliveryRecordRepository _contentDeliveryRecordPersistanceService;

        public ContentDeliveryRecordsController(ILogger<ContentDeliveryRecordsController> logger, IContentDeliveryRecordRepository contentDeliveryRecordPersistanceService)
        {
            this._logger = logger;
            this._contentDeliveryRecordPersistanceService = contentDeliveryRecordPersistanceService;
        }
        [HttpGet]
        public async Task<ActionResult<List<ContentDeliveryRecord>>> GetAsync(string resourcegroup)
        {
            try
            {
                return await this._contentDeliveryRecordPersistanceService.GetAllAsync(resourcegroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ContentDeliveryRecord>();
            }
        }

        [HttpGet("ByBackupRecordId/{id}")]
        public async Task<ActionResult<List<ContentDeliveryRecord>>> GeByBackupRecordIdAsync(string id)
        {
            try
            {
                return await this._contentDeliveryRecordPersistanceService.GetAllByBackupRecordIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ContentDeliveryRecord>();
            }
        }

        [Route("re-run/{id}")]
        [HttpGet, HttpPost]
        public async Task<ActionResult> GetInitRerunAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var contentDeliveryRecord = await _contentDeliveryRecordPersistanceService.GetByIdAsync(id);
                if (contentDeliveryRecord == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else if (contentDeliveryRecord.CurrentStatus != "ERROR")
                    return new ConflictObjectResult($"STATUS need to be ERROR, Current Status for this record is: {contentDeliveryRecord.CurrentStatus}");
                bool rerunSuccess = await _contentDeliveryRecordPersistanceService.UpdateStatusFeedAsync(id, ContentDeliveryRecordStatus.QUEUED.ToString(), "Queued for Re-run", 0);
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
