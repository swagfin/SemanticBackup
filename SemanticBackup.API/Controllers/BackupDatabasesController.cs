using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.API.Models.Response;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [Authorize]
    [Route("{resourcegroup}/api/[controller]")]
    [ApiController]
    public class BackupDatabasesController : ControllerBase
    {
        private readonly ILogger<BackupDatabasesController> _logger;
        private readonly IDatabaseInfoRepository _backupDatabasePersistanceService;
        private readonly IBackupScheduleRepository _schedulePersistanceService;
        private readonly IBackupProviderForMySQLServer _mySQLServerBackupProviderService;
        private readonly IBackupProviderForSQLServer _sQLServerBackupProviderService;

        public BackupDatabasesController(ILogger<BackupDatabasesController> logger, IDatabaseInfoRepository databaseInfoPersistanceService, IBackupScheduleRepository schedulePersistanceService, IBackupProviderForMySQLServer mySQLServerBackupProviderService, IBackupProviderForSQLServer sQLServerBackupProviderService)
        {
            _logger = logger;
            this._backupDatabasePersistanceService = databaseInfoPersistanceService;
            this._schedulePersistanceService = schedulePersistanceService;
            this._mySQLServerBackupProviderService = mySQLServerBackupProviderService;
            this._sQLServerBackupProviderService = sQLServerBackupProviderService;
        }
        [HttpGet]
        public async Task<ActionResult<List<BackupDatabaseInfoResponse>>> GetAsync(string resourcegroup)
        {
            try
            {
                var records = await _backupDatabasePersistanceService.GetAllAsync(resourcegroup);
                if (records == null)
                    return new List<BackupDatabaseInfoResponse>();
                return records.ToList().Select(x => new BackupDatabaseInfoResponse
                {
                    DatabaseName = x.DatabaseName,
                    DatabaseType = x.DatabaseType,
                    Description = x.Description,
                    Id = x.Id,
                    Port = x.Port,
                    Server = x.Server,
                    Username = x.Username,
                    Password = GetInvisibleText(x.Password),
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupDatabaseInfoResponse>();
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BackupDatabaseInfoResponse>> GetRecordAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var x = await _backupDatabasePersistanceService.GetByIdAsync(id);
                if (x == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
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

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] BackupDatabaseRequest request, string resourcegroup)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                    return new BadRequestObjectResult("No Databases Provided");
                List<string> databases = request.DatabaseName.Split(',').ToList();
                foreach (var database in databases)
                {
                    BackupDatabaseInfo saveObj = new BackupDatabaseInfo
                    {
                        ResourceGroupId = resourcegroup,
                        Server = request.Server,
                        DatabaseName = database,
                        Username = request.Username,
                        Password = request.Password,
                        DatabaseType = request.DatabaseType,
                        Port = request.Port,
                        Description = request.Description,
                        DateRegisteredUTC = DateTime.UtcNow
                    };
                    bool savedSuccess = await _backupDatabasePersistanceService.AddOrUpdateAsync(saveObj);
                    if (!savedSuccess)
                        throw new Exception("Data was not Saved");
                    if (request.AutoCreateSchedule)
                        await CreateScheduleForAsync(saveObj);
                }
                return Ok();
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
                bool savedSuccess = await _schedulePersistanceService.AddOrUpdateAsync(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Auto Create Daily Backup for Database Key: {databaseInfo.Id},Error: {ex.Message}"); }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutAsync([FromBody] BackupDatabaseRequest request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var savedObj = await _backupDatabasePersistanceService.GetByIdAsync(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Update Params
                savedObj.Server = request.Server;
                savedObj.DatabaseName = request.DatabaseName;
                savedObj.Username = request.Username;
                if (!string.IsNullOrWhiteSpace(request.Password))
                    savedObj.Password = request.Password;
                savedObj.DatabaseType = request.DatabaseType;
                savedObj.Port = request.Port;
                savedObj.Description = request.Description;

                bool updatedSuccess = await _backupDatabasePersistanceService.UpdateAsync(savedObj);
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
                bool removedSuccess = await _backupDatabasePersistanceService.RemoveAsync(id);
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

        [HttpPost("pre-get-database-collection")]
        public async Task<IEnumerable<string>> GetPreGetDbCollectionAsync([FromForm] DatabaseCollectionRequest request)
        {
            try
            {
                if (request == null)
                    return new List<string>();
                //Checks
                if (string.IsNullOrWhiteSpace(request.Server) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return null;
                var dbInfo = new BackupDatabaseInfo
                {
                    Server = request.Server,
                    Username = request.Username,
                    Password = request.Password,
                    DatabaseType = request.Type,
                    Port = request.Port,
                };

                if (request.Type.Contains("SQLSERVER"))
                    return await _sQLServerBackupProviderService.GetAvailableDatabaseCollectionAsync(dbInfo);
                else if (request.Type.Contains("MYSQL") || request.Type.Contains("MARIADB"))
                    return await _mySQLServerBackupProviderService.GetAvailableDatabaseCollectionAsync(dbInfo);
                else
                    throw new Exception($"No Backup Service registered to Handle Database Query of Type: {request.Type}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return null;
            }
        }
    }
}
