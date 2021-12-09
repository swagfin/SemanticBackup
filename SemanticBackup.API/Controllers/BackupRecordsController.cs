using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class BackupRecordsController : ControllerBase
    {
        private readonly ILogger<BackupRecordsController> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly SharedTimeZone sharedTimeZone;

        public BackupRecordsController(ILogger<BackupRecordsController> logger, IBackupRecordPersistanceService backupRecordPersistanceService, PersistanceOptions persistanceOptions, IDatabaseInfoPersistanceService databaseInfoPersistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._persistanceOptions = persistanceOptions;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this.sharedTimeZone = sharedTimeZone;
        }

        [HttpGet]
        public ActionResult<List<BackupRecord>> Get(string resourcegroup)
        {
            try
            {
                return _backupRecordPersistanceService.GetAll(resourcegroup);
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
        public ActionResult<BackupRecord> GetById(string id)
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
        [Route("re-run/{id}")]
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

        [Route("request-instant-backup/{id}")]
        [HttpGet, HttpPost]
        public ActionResult<BackupRecord> GetRequestInstantBackup(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var backupDatabaseInfo = _databaseInfoPersistanceService.GetById(id);
                if (backupDatabaseInfo == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");

                //Check if an Existing Queued
                var queuedExisting = this._backupRecordPersistanceService.GetAllByDatabaseIdByStatus(backupDatabaseInfo.ResourceGroupId, backupDatabaseInfo.Id, BackupRecordBackupStatus.QUEUED.ToString());
                if (queuedExisting != null && queuedExisting.Count > 0)
                {
                    //No Need to Create another Just Return
                    return queuedExisting.FirstOrDefault();
                }
                //Proceed Otherwise
                DateTime currentTime = sharedTimeZone.Now;
                DateTime? RecordExpiry = null;
                if (backupDatabaseInfo.BackupExpiryAgeInDays >= 1)
                    RecordExpiry = currentTime.AddDays(backupDatabaseInfo.BackupExpiryAgeInDays);
                BackupRecord newRecord = new BackupRecord
                {
                    BackupDatabaseInfoId = backupDatabaseInfo.Id,
                    ResourceGroupId = backupDatabaseInfo.ResourceGroupId,
                    BackupStatus = BackupRecordBackupStatus.QUEUED.ToString(),
                    ExpiryDate = RecordExpiry,
                    Name = backupDatabaseInfo.Name,
                    Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, SharedFunctions.GetSavingPathFromFormat(backupDatabaseInfo, _persistanceOptions.BackupFileSaveFormat, currentTime)),
                    StatusUpdateDate = currentTime,
                    RegisteredDate = currentTime
                };
                bool addedSuccess = this._backupRecordPersistanceService.AddOrUpdate(newRecord);
                if (addedSuccess)
                    return newRecord;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
