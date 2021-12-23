﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Models.Response;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class BackupRecordsController : ControllerBase
    {
        private readonly ILogger<BackupRecordsController> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;
        private readonly IResourceGroupPersistanceService _resourceGroupPersistanceService;
        private readonly PersistanceOptions _persistanceOptions;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;

        public BackupRecordsController(ILogger<BackupRecordsController> logger, IBackupRecordPersistanceService backupRecordPersistanceService, IResourceGroupPersistanceService resourceGroupPersistanceService, PersistanceOptions persistanceOptions, IDatabaseInfoPersistanceService databaseInfoPersistanceService)
        {
            _logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this._persistanceOptions = persistanceOptions;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BackupRecordResponse>>> GetAsync(string resourcegroup)
        {
            try
            {
                List<BackupRecord> records = await _backupRecordPersistanceService.GetAllAsync(resourcegroup);
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(resourcegroup);
                return records.Select(x => new BackupRecordResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    BackupStatus = x.BackupStatus,
                    ExecutionMessage = x.ExecutionMessage,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    ExecutionMilliseconds = x.ExecutionMilliseconds,
                    Path = x.Path,
                    ExecutedDeliveryRun = x.ExecutedDeliveryRun,
                    ExpiryDate = x.ExpiryDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    RegisteredDate = x.RegisteredDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StatusUpdateDate = x.StatusUpdateDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecordResponse>();
            }
        }
        [HttpGet("ByDatabaseId/{id}")]
        public async Task<ActionResult<List<BackupRecordResponse>>> GetByDatabaseIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new BadRequestObjectResult("Database Id can't be Null or Empty");
                BackupDatabaseInfo validDatabase = await _databaseInfoPersistanceService.GetByIdAsync(id);
                if (validDatabase == null)
                    return new List<BackupRecordResponse>();
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(validDatabase.ResourceGroupId);
                //Get Records By Database Id
                List<BackupRecord> records = await _backupRecordPersistanceService.GetAllByDatabaseIdAsync(id);
                return records.Select(x => new BackupRecordResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    BackupStatus = x.BackupStatus,
                    ExecutionMessage = x.ExecutionMessage,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    ExecutionMilliseconds = x.ExecutionMilliseconds,
                    Path = x.Path,
                    ExecutedDeliveryRun = x.ExecutedDeliveryRun,
                    ExpiryDate = x.ExpiryDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    RegisteredDate = x.RegisteredDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StatusUpdateDate = x.StatusUpdateDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecordResponse>();
            }
        }

        [HttpGet("ByStatus/{status}")]
        public async Task<ActionResult<List<BackupRecordResponse>>> GetByStatusAsync(string status, string resourcegroup)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return new BadRequestObjectResult("Status was not Provided");
                var records = await _backupRecordPersistanceService.GetAllByStatusAsync(status);
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(resourcegroup);
                return records.Select(x => new BackupRecordResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    BackupStatus = x.BackupStatus,
                    ExecutionMessage = x.ExecutionMessage,
                    BackupDatabaseInfoId = x.BackupDatabaseInfoId,
                    ExecutionMilliseconds = x.ExecutionMilliseconds,
                    Path = x.Path,
                    ExecutedDeliveryRun = x.ExecutedDeliveryRun,
                    ExpiryDate = x.ExpiryDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    RegisteredDate = x.RegisteredDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StatusUpdateDate = x.StatusUpdateDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecordResponse>();
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BackupRecordResponse>> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                BackupRecord record = await _backupRecordPersistanceService.GetByIdAsync(id);
                if (record == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(record.ResourceGroupId);
                return new BackupRecordResponse
                {
                    Id = record.Id,
                    Name = record.Name,
                    BackupStatus = record.BackupStatus,
                    ExecutionMessage = record.ExecutionMessage,
                    BackupDatabaseInfoId = record.BackupDatabaseInfoId,
                    ExecutionMilliseconds = record.ExecutionMilliseconds,
                    Path = record.Path,
                    ExecutedDeliveryRun = record.ExecutedDeliveryRun,
                    ExpiryDate = record.ExpiryDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    RegisteredDate = record.RegisteredDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                    StatusUpdateDate = record.StatusUpdateDateUTC.ConvertFromUTC(resourceGroup?.TimeZone),
                };
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
                bool removedSuccess = await _backupRecordPersistanceService.RemoveAsync(id);
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

        [Route("re-run/{id}")]
        [HttpGet, HttpPost]
        public async Task<ActionResult> GetInitRerunAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var backupRecord = await _backupRecordPersistanceService.GetByIdAsync(id);
                if (backupRecord == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else if (backupRecord.BackupStatus != "ERROR")
                    return new ConflictObjectResult($"STATUS need to be ERROR, Current Status for this record is: {backupRecord.BackupStatus}");
                string newBackupPath = backupRecord.Path.Replace(".zip", ".bak");
                bool rerunSuccess = await _backupRecordPersistanceService.UpdateStatusFeedAsync(id, BackupRecordBackupStatus.QUEUED.ToString(), "Queued for Re-run", 0, newBackupPath);
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
        public async Task<ActionResult<BackupRecord>> GetRequestInstantBackupAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var backupDatabaseInfo = await _databaseInfoPersistanceService.GetByIdAsync(id);
                if (backupDatabaseInfo == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Check if an Existing Queued
                var queuedExisting = await this._backupRecordPersistanceService.GetAllByDatabaseIdByStatusAsync(backupDatabaseInfo.ResourceGroupId, backupDatabaseInfo.Id, BackupRecordBackupStatus.QUEUED.ToString());
                if (queuedExisting != null && queuedExisting.Count > 0)
                {
                    //No Need to Create another Just Return
                    return queuedExisting.FirstOrDefault();
                }
                //Resource Group
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdAsync(backupDatabaseInfo?.ResourceGroupId);
                //Proceed Otherwise
                DateTime currentTimeUTC = DateTime.UtcNow;
                DateTime currentTimeLocal = currentTimeUTC.ConvertFromUTC(resourceGroup?.TimeZone);
                DateTime RecordExpiryUTC = currentTimeUTC.AddDays(resourceGroup.BackupExpiryAgeInDays);
                BackupRecord newRecord = new BackupRecord
                {
                    BackupDatabaseInfoId = backupDatabaseInfo.Id,
                    ResourceGroupId = backupDatabaseInfo.ResourceGroupId,
                    BackupStatus = BackupRecordBackupStatus.QUEUED.ToString(),
                    ExpiryDateUTC = RecordExpiryUTC,
                    Name = backupDatabaseInfo.Name,
                    Path = Path.Combine(_persistanceOptions.DefaultBackupDirectory, SharedFunctions.GetSavingPathFromFormat(backupDatabaseInfo, _persistanceOptions.BackupFileSaveFormat, currentTimeLocal)),
                    StatusUpdateDateUTC = currentTimeUTC,
                    RegisteredDateUTC = currentTimeUTC,
                    ExecutedDeliveryRun = false
                };
                bool addedSuccess = await this._backupRecordPersistanceService.AddOrUpdateAsync(newRecord);
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
