using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicController : ControllerBase
    {
        private readonly ILogger<PublicController> _logger;
        private readonly IDatabaseInfoRepository _databaseInfoPersistanceService;
        private readonly IResourceGroupRepository _resourceGroupPersistanceService;
        private readonly IBackupRecordRepository _backupRecordPersistanceService;
        private readonly Core.SystemConfigOptions _persistanceOptions;
        private readonly IBackupScheduleRepository _backupSchedulePersistanceService;
        private readonly SystemConfigOptions _options;

        public PublicController(ILogger<PublicController> logger,
            IDatabaseInfoRepository databaseInfoPersistanceService,
            IResourceGroupRepository resourceGroupPersistanceService, IBackupRecordRepository backupRecordPersistanceService, Core.SystemConfigOptions persistanceOptions, IBackupScheduleRepository backupSchedulePersistanceService, SystemConfigOptions options)
        {
            this._logger = logger;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._resourceGroupPersistanceService = resourceGroupPersistanceService;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
            this._persistanceOptions = persistanceOptions;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._options = options;
        }

        private void VerifyAuthTokenProvided(string token)
        {
            if (!string.Equals(token, _options.PublicAccessToken))
                throw new Exception($"Invalid Access Token: {token}");
        }
        [Route("CheckDatabase/{type}/{db}"), HttpGet]
        public async Task<ActionResult<BackupDatabaseInfoResponse>> CheckDatabaseInfo(BackupDatabaseInfoDbType type, string db, string token = "")
        {
            try
            {
                VerifyAuthTokenProvided(token);
                //proceed
                if (string.IsNullOrWhiteSpace(db))
                    throw new Exception("Database Name can't be NULL");
                var x = await _databaseInfoPersistanceService.GetByDatabaseNameAsync(db, type.ToString());
                if (x == null)
                    return new NotFoundObjectResult($"No Data Found with Database Name: {db}");
                return new BackupDatabaseInfoResponse
                {
                    DatabaseName = x.DatabaseName,
                    DatabaseType = x.DatabaseType,
                    Description = x.Description,
                    Id = x.Id,
                    Port = x.Port,
                    Server = x.Server,
                    Username = x.Username,
                    Password = GetInvisibleText(x.Password),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Route("BackupRecords/{type}/{db}"), HttpGet]
        public async Task<ActionResult<List<BackupRecordResponse>>> GetBackupRecords(BackupDatabaseInfoDbType type, string db, int limit = 50, string token = "")
        {
            try
            {
                VerifyAuthTokenProvided(token);
                //proceed
                var validDatabase = await _databaseInfoPersistanceService.GetByDatabaseNameAsync(db, type.ToString());
                if (validDatabase == null)
                    return new List<BackupRecordResponse>();
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdOrKeyAsync(validDatabase.ResourceGroupId);
                //Get Records By Database Id
                List<BackupRecord> records = await _backupRecordPersistanceService.GetAllByDatabaseIdAsync(validDatabase.Id);
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
                }).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecordResponse>();
            }
        }

        [Route("RequestInstantBackup/{type}/{db}"), HttpGet]
        public async Task<ActionResult<BackupRecord>> GetInstantBackup(BackupDatabaseInfoDbType type, string db, string token = "")
        {
            try
            {
                VerifyAuthTokenProvided(token);
                //proceed
                if (string.IsNullOrWhiteSpace(db))
                    throw new Exception("Id can't be NULL");
                BackupDatabaseInfo backupDatabaseInfo = await _databaseInfoPersistanceService.GetByDatabaseNameAsync(db, type.ToString());
                if (backupDatabaseInfo == null)
                    return new NotFoundObjectResult($"No Database Found with name: {db}");
                //Get Resource Group
                //Check if an Existing Queued
                var queuedExisting = await this._backupRecordPersistanceService.GetAllByDatabaseIdByStatusAsync(backupDatabaseInfo.ResourceGroupId, backupDatabaseInfo.Id, BackupRecordBackupStatus.QUEUED.ToString());
                if (queuedExisting != null && queuedExisting.Count > 0)
                {
                    //No Need to Create another Just Return
                    return queuedExisting.FirstOrDefault();
                }
                //Resource Group
                ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdOrKeyAsync(backupDatabaseInfo?.ResourceGroupId);
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
                    Path = System.IO.Path.Combine(_persistanceOptions.DefaultBackupDirectory, SharedFunctions.GetSavingPathFromFormat(backupDatabaseInfo, _persistanceOptions.BackupFileSaveFormat, currentTimeLocal)),
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

        [Route("SubscribeBackupFeature/{token}"), HttpPost]
        public async Task<ActionResult<BackupDatabaseInfoResponse>> PostSubscribeBackupFeature([FromForm] BackupDatabaseRequest request, string token = "")
        {
            try
            {
                VerifyAuthTokenProvided(token);
                //proceed
                var x = await _databaseInfoPersistanceService.GetByDatabaseNameAsync(request.DatabaseName, request.DatabaseType);
                if (x != null)
                    return new BackupDatabaseInfoResponse
                    {
                        DatabaseName = x.DatabaseName,
                        DatabaseType = x.DatabaseType,
                        Description = x.Description,
                        Id = x.Id,
                        Port = x.Port,
                        Server = x.Server,
                        Username = x.Username,
                        Password = GetInvisibleText(x.Password),
                    };

                var resourceGroups = await _resourceGroupPersistanceService.GetAllAsync();
                var resourceGrp = resourceGroups.FirstOrDefault();
                if (resourceGrp == null)
                    throw new Exception("No Registered Resource Groups Available at this Time");
                //Else Try Create It
                x = new BackupDatabaseInfo
                {
                    ResourceGroupId = resourceGrp.Id,
                    Server = request.Server,
                    DatabaseName = request.DatabaseName.Trim(),
                    Username = request.Username,
                    Password = request.Password,
                    DatabaseType = request.DatabaseType,
                    Port = request.Port,
                    Description = request.Description,
                    DateRegisteredUTC = DateTime.UtcNow
                };
                bool savedSuccess = await _databaseInfoPersistanceService.AddOrUpdateAsync(x);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
                if (request.AutoCreateSchedule)
                    await CreateScheduleForAsync(x);

                //Return Object
                return new BackupDatabaseInfoResponse
                {
                    DatabaseName = x.DatabaseName,
                    DatabaseType = x.DatabaseType,
                    Description = x.Description,
                    Id = x.Id,
                    Port = x.Port,
                    Server = x.Server,
                    Username = x.Username,
                    Password = GetInvisibleText(x.Password),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private async Task CreateScheduleForAsync(BackupDatabaseInfo databaseInfo)
        {
            try
            {
                DateTime currentTimeUTC = DateTime.UtcNow;
                BackupSchedule saveObj = new BackupSchedule
                {
                    BackupDatabaseInfoId = databaseInfo.Id,
                    ResourceGroupId = databaseInfo.ResourceGroupId,
                    ScheduleType = BackupScheduleType.FULLBACKUP.ToString(),
                    EveryHours = 24,
                    StartDateUTC = new DateTime(currentTimeUTC.Year, currentTimeUTC.Month, currentTimeUTC.Day + 1),
                    CreatedOnUTC = currentTimeUTC,
                    Name = databaseInfo.Name
                };
                bool savedSuccess = await _backupSchedulePersistanceService.AddOrUpdateAsync(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Auto Create Daily Backup for Database Key: {databaseInfo.Id},Error: {ex.Message}"); }
        }
        private string GetInvisibleText(string password)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                    return string.Empty;
                string finalCode = string.Empty;
                foreach (char c in password)
                    finalCode = $"{finalCode}*";
                return finalCode;
            }
            catch { }
            return string.Empty;
        }
    }
}
