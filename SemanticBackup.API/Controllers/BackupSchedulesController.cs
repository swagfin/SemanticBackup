using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.API.Models.Response;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class BackupSchedulesController : ControllerBase
    {
        private readonly ILogger<BackupSchedulesController> _logger;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly SharedTimeZone _sharedTimeZone;

        public BackupSchedulesController(ILogger<BackupSchedulesController> logger, IBackupSchedulePersistanceService persistanceService, IResourceGroupPersistanceService resourceGroupPersistanceService, IDatabaseInfoPersistanceService databaseInfoPersistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupSchedulePersistanceService = persistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
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
        public ActionResult<List<BackupScheduleResponse>> Get(string resourcegroup)
        {
            try
            {
                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(resourcegroup);
                var records = _backupSchedulePersistanceService.GetAll(resourcegroup);
                return records.Select(x => new BackupScheduleResponse
                {
                    Id = x.Id,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    EveryHours = x.EveryHours,
                    Name = x.Name,
                    ScheduleType = x.ScheduleType,
                    LastRun = x.LastRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    NextRun = x.NextRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StartDate = x.StartDateUTC.ConvertFromUTC(resourceGroup?.TimeZone)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupScheduleResponse>();
            }
        }

        [HttpGet("CurrentDue")]
        public ActionResult<List<BackupScheduleResponse>> GetCurrentDue(string resourcegroup)
        {
            try
            {
                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(resourcegroup);
                var records = _backupSchedulePersistanceService.GetAllDueByDate();
                return records.Select(x => new BackupScheduleResponse
                {
                    Id = x.Id,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    EveryHours = x.EveryHours,
                    Name = x.Name,
                    ScheduleType = x.ScheduleType,
                    LastRun = x.LastRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    NextRun = x.NextRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StartDate = x.StartDateUTC.ConvertFromUTC(resourceGroup?.TimeZone)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupScheduleResponse>();
            }
        }

        [HttpGet("ByDatabaseId/{id}")]
        public ActionResult<List<BackupScheduleResponse>> GetByDatabaseId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new BadRequestObjectResult("Database Id can't be Null or Empty");
                var backupDatabaseInfo = _databaseInfoPersistanceService.GetById(id);
                if (backupDatabaseInfo == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                var records = _backupSchedulePersistanceService.GetAllByDatabaseId(id);
                //GET Db Resource Group
                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupDatabaseInfo.ResourceGroupId);
                return records.Select(x => new BackupScheduleResponse
                {
                    Id = x.Id,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    EveryHours = x.EveryHours,
                    Name = x.Name,
                    ScheduleType = x.ScheduleType,
                    LastRun = x.LastRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    NextRun = x.NextRunUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StartDate = x.StartDateUTC.ConvertFromUTC(resourceGroup?.TimeZone)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupScheduleResponse>();
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
                BackupDatabaseInfo backupDatabaseInfo = VerifyDatabaseExistsById(request.BackupDatabaseInfoId);
                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupDatabaseInfo.ResourceGroupId);
                //Get Times By Timezone
                DateTime currentTimeUTC = DateTime.UtcNow;
                BackupSchedule saveObj = new BackupSchedule
                {
                    BackupDatabaseInfoId = request.BackupDatabaseInfoId,
                    ResourceGroupId = backupDatabaseInfo.ResourceGroupId,
                    ScheduleType = request.ScheduleType,
                    EveryHours = request.EveryHours,
                    StartDateUTC = request.StartDate.ConvertToUTC(resourceGroup?.TimeZone),
                    CreatedOnUTC = currentTimeUTC,
                    Name = backupDatabaseInfo.Name,
                };
                bool savedSuccess = _backupSchedulePersistanceService.AddOrUpdate(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
                return Ok();
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
                BackupDatabaseInfo backupDatabaseInfo = VerifyDatabaseExistsById(request.BackupDatabaseInfoId);
                //Proceed
                var savedObj = _backupSchedulePersistanceService.GetById(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");

                ResourceGroup resourceGroup = _resourceGroupPersistanceService.GetById(backupDatabaseInfo.ResourceGroupId);
                //Update Params
                savedObj.ResourceGroupId = backupDatabaseInfo.ResourceGroupId;
                savedObj.BackupDatabaseInfoId = request.BackupDatabaseInfoId;
                savedObj.ScheduleType = request.ScheduleType;
                savedObj.EveryHours = request.EveryHours;
                savedObj.StartDateUTC = request.StartDate.ConvertToUTC(resourceGroup?.TimeZone);
                savedObj.Name = backupDatabaseInfo.Name;
                bool updatedSuccess = _backupSchedulePersistanceService.Update(savedObj);
                if (!updatedSuccess)
                    throw new Exception("Data was not Updated");
                return Ok();
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
