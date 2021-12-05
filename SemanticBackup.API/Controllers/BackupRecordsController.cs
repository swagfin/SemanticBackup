using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
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
        private readonly SharedTimeZone sharedTimeZone;

        public BackupRecordsController(ILogger<BackupRecordsController> logger, IBackupRecordPersistanceService backupRecordPersistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this.sharedTimeZone = sharedTimeZone;
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

        [HttpGet("{id}")]
        public ActionResult<BackupRecord> Get(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var backupRecord = _backupRecordPersistanceService.GetById(id);
                if (backupRecord == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                return backupRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
        [Route("{id}/re-run")]
        [HttpGet, HttpPost]
        public ActionResult<BackupRecord> GetInitRerun(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var backupRecord = _backupRecordPersistanceService.GetById(id);
                if (backupRecord == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else if (backupRecord.BackupStatus != "ERROR")
                    return new ConflictObjectResult($"STATUS need to be ERROR, Current Status for this record is: {backupRecord.BackupStatus}");
                DateTime startedAt = sharedTimeZone.Now;
                string newBackupPath = backupRecord.Path.Replace(".zip", ".bak");
                bool rerunSuccess = _backupRecordPersistanceService.UpdateStatusFeed(id, BackupRecordBackupStatus.QUEUED.ToString(), startedAt, "Queued for Re-run", 0, newBackupPath);
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
