using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.API.Models.Response;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class BackupSchedulesController : ControllerBase
    {
        private readonly ILogger<BackupSchedulesController> _logger;
        private readonly IBackupScheduleRepository _backupSchedulePersistanceService;
        private readonly IResourceGroupRepository _resourceGroupPersistanceService;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;

        public BackupSchedulesController(ILogger<BackupSchedulesController> logger, IBackupScheduleRepository persistanceService, IResourceGroupRepository resourceGroupPersistanceService, IDatabaseInfoRepository databaseInfoPersistanceService)
        {
            _logger = logger;
            this._backupSchedulePersistanceService = persistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }

        private async Task<BackupDatabaseInfo> VerifyDatabaseExistsByIdAsync(string backupDatabaseInfoId)
        {
            var databaseRecord = await this._databaseInfoPersistanceService.GetByIdAsync(backupDatabaseInfoId);
            if (databaseRecord == null)
                throw new Exception($"No Such Device with ID: {backupDatabaseInfoId}");
            return databaseRecord;
        }

        [HttpGet]
        public async Task<ActionResult<List<BackupScheduleResponse>>> GetAsync(string resourcegroup)
        {
            try
            {
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(resourcegroup);
                var records = await _backupSchedulePersistanceService.GetAllAsync(resourcegroup);
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
        public async Task<ActionResult<List<BackupScheduleResponse>>> GetCurrentDueAsync(string resourcegroup)
        {
            try
            {
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(resourcegroup);
                var records = await _backupSchedulePersistanceService.GetAllDueByDateAsync();
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
        public async Task<ActionResult<List<BackupScheduleResponse>>> GetByDatabaseIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new BadRequestObjectResult("Database Id can't be Null or Empty");
                var backupDatabaseInfo = await _databaseInfoPersistanceService.GetByIdAsync(id);
                if (backupDatabaseInfo == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                var records = await _backupSchedulePersistanceService.GetAllByDatabaseIdAsync(id);
                //GET Db Resource Group
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo.ResourceGroupId);
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
        public async Task<ActionResult> PostAsync([FromBody] BackupScheduleRequest request)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                //Verify Database Info Exists
                BackupDatabaseInfo backupDatabaseInfo = await VerifyDatabaseExistsByIdAsync(request.BackupDatabaseInfoId);
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo.ResourceGroupId);
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
                bool savedSuccess = await _backupSchedulePersistanceService.AddOrUpdateAsync(saveObj);
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
        public async Task<ActionResult> PutAsync([FromBody] BackupScheduleRequest request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Verify Database Info Exists
                BackupDatabaseInfo backupDatabaseInfo = await VerifyDatabaseExistsByIdAsync(request.BackupDatabaseInfoId);
                //Proceed
                var savedObj = await _backupSchedulePersistanceService.GetByIdAsync(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Proceed
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo.ResourceGroupId);
                //Update Params
                savedObj.ResourceGroupId = backupDatabaseInfo.ResourceGroupId;
                savedObj.BackupDatabaseInfoId = request.BackupDatabaseInfoId;
                savedObj.ScheduleType = request.ScheduleType;
                savedObj.EveryHours = request.EveryHours;
                savedObj.StartDateUTC = request.StartDate.ConvertToUTC(resourceGroup?.TimeZone);
                savedObj.Name = backupDatabaseInfo.Name;
                bool updatedSuccess = await _backupSchedulePersistanceService.UpdateAsync(savedObj);
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
        public async Task<ActionResult> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Update Params
                bool removedSuccess = await _backupSchedulePersistanceService.RemoveAsync(id);
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
