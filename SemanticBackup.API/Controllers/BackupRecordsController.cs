using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupRecordsController : ControllerBase
    {
        private readonly ILogger<BackupRecordsController> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;

        public BackupRecordsController(ILogger<BackupRecordsController> logger, IBackupRecordPersistanceService backupRecordPersistanceService)
        {
            _logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
        }

        [HttpGet]
        public ActionResult<List<BackupRecord>> Get()
        {
            try
            {
                return _backupRecordPersistanceService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecord>();
            }
        }
        [HttpGet("ByDatabaseId/{id}")]
        public ActionResult<List<BackupRecord>> GetByDatabaseId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new BadRequestObjectResult("Database Id can't be Null or Empty");
                return _backupRecordPersistanceService.GetAllByDatabaseId(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecord>();
            }
        }

        [HttpGet("ByStatus/{status}")]
        public ActionResult<List<BackupRecord>> GetByStatus(string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return new BadRequestObjectResult("Status was not Provided");
                return _backupRecordPersistanceService.GetAllByStatus(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecord>();
            }
        }
    }
}
