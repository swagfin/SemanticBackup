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
    }
}
