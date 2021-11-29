using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupSchedulesController : ControllerBase
    {
        private readonly ILogger<BackupSchedulesController> _logger;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly SharedTimeZone _sharedTimeZone;

        public BackupSchedulesController(ILogger<BackupSchedulesController> logger, IBackupSchedulePersistanceService persistanceService, IDatabaseInfoPersistanceService databaseInfoPersistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupSchedulePersistanceService = persistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._sharedTimeZone = sharedTimeZone;
        }

        private BackupDatabaseInfo VerifyDatabaseExistsById(string backupDatabaseInfoId)
        {
            var databaseRecord = this._databaseInfoPersistanceService.GetById(backupDatabaseInfoId);
            if (databaseRecord == null)
                throw new Exception($"No Such Device with ID: {backupDatabaseInfoId}");
            return databaseRecord;
        }

        [HttpGet]
        public ActionResult<List<BackupSchedule>> Get()
        {
            try
            {
                return _backupSchedulePersistanceService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupSchedule>();
            }
        }

        [HttpGet("CurrentDue")]
        public ActionResult<List<BackupSchedule>> GetCurrentDue()
        {
            try
            {
                DateTime currentTime = _sharedTimeZone.Now;
                return _backupSchedulePersistanceService.GetAllDueByDate(currentTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupSchedule>();
            }
        }

        [HttpGet("ByDatabaseId/{id}")]
        public ActionResult<List<BackupSchedule>> GetByDatabaseId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new BadRequestObjectResult("Database Id can't be Null or Empty");
                return _backupSchedulePersistanceService.GetAllByDatabaseId(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupSchedule>();
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] BackupScheduleRequest request)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                //Verify Database Info Exists
                var record = VerifyDatabaseExistsById(request.BackupDatabaseInfoId);
                DateTime currentTime = _sharedTimeZone.Now;
                BackupSchedule saveObj = new BackupSchedule
                {
                    BackupDatabaseInfoId = request.BackupDatabaseInfoId,
                    ScheduleType = request.ScheduleType,
                    EveryHours = request.EveryHours,
                    StartDate = request.StartDate,
                    CreatedOn = currentTime,
                    Name = record.Name
                };
                bool savedSuccess = _backupSchedulePersistanceService.AddOrUpdate(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
                return Ok(saveObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public ActionResult Put([FromBody] BackupScheduleRequest request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Verify Database Info Exists
                var record = VerifyDatabaseExistsById(request.BackupDatabaseInfoId);
                //Proceed
                var savedObj = _backupSchedulePersistanceService.GetById(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Update Params
                savedObj.BackupDatabaseInfoId = request.BackupDatabaseInfoId;
                savedObj.ScheduleType = request.ScheduleType;
                savedObj.EveryHours = request.EveryHours;
                savedObj.StartDate = request.StartDate;
                savedObj.Name = record.Name;
                bool updatedSuccess = _backupSchedulePersistanceService.Update(savedObj);
                if (!updatedSuccess)
                    throw new Exception("Data was not Updated");
                return Ok(savedObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Update Params
                bool removedSuccess = _backupSchedulePersistanceService.Remove(id);
                if (!removedSuccess)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else
                    return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
