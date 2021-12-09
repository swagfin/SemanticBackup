using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.API.Models.Response;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("{directory}/api/[controller]")]
    public class BackupDatabasesController : ControllerBase
    {
        private readonly ILogger<BackupDatabasesController> _logger;
        private readonly IDatabaseInfoPersistanceService _backupDatabasePersistanceService;
        private readonly IBackupSchedulePersistanceService _schedulePersistanceService;
        private readonly IMySQLServerBackupProviderService _mySQLServerBackupProviderService;
        private readonly ISQLServerBackupProviderService _sQLServerBackupProviderService;
        private readonly SharedTimeZone _sharedTimeZone;

        public BackupDatabasesController(ILogger<BackupDatabasesController> logger, IDatabaseInfoPersistanceService databaseInfoPersistanceService, IBackupSchedulePersistanceService schedulePersistanceService, IMySQLServerBackupProviderService mySQLServerBackupProviderService, ISQLServerBackupProviderService sQLServerBackupProviderService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupDatabasePersistanceService = databaseInfoPersistanceService;
            this._schedulePersistanceService = schedulePersistanceService;
            this._mySQLServerBackupProviderService = mySQLServerBackupProviderService;
            this._sQLServerBackupProviderService = sQLServerBackupProviderService;
            this._sharedTimeZone = sharedTimeZone;
        }

        [HttpGet]
        public ActionResult<List<BackupDatabaseInfoResponse>> Get(string directory)
        {
            try
            {
                var records = _backupDatabasePersistanceService.GetAll(directory);
                if (records == null)
                    return new List<BackupDatabaseInfoResponse>();
                return records.ToList().Select(x => new BackupDatabaseInfoResponse
                {
                    BackupExpiryAgeInDays = x.BackupExpiryAgeInDays,
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
        public ActionResult<BackupDatabaseInfoResponse> GetRecord(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var x = _backupDatabasePersistanceService.GetById(id);
                if (x == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                return new BackupDatabaseInfoResponse
                {
                    BackupExpiryAgeInDays = x.BackupExpiryAgeInDays,
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
        public ActionResult Post([FromBody] BackupDatabaseRequest request, string directory)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                    return new BadRequestObjectResult("No Databases Provided");
                DateTime currentTime = _sharedTimeZone.Now;
                List<string> databases = request.DatabaseName.Split(',').ToList();
                foreach (var database in databases)
                {
                    BackupDatabaseInfo saveObj = new BackupDatabaseInfo
                    {
                        ActiveDirectoryId = directory,
                        Server = request.Server,
                        DatabaseName = database,
                        Username = request.Username,
                        Password = request.Password,
                        DatabaseType = request.DatabaseType,
                        Port = request.Port,
                        Description = request.Description,
                        DateRegistered = currentTime,
                        BackupExpiryAgeInDays = request.BackupExpiryAgeInDays
                    };
                    bool savedSuccess = _backupDatabasePersistanceService.AddOrUpdate(saveObj);
                    if (!savedSuccess)
                        throw new Exception("Data was not Saved");
                    if (request.AutoCreateSchedule)
                        CreateScheduleFor(saveObj);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private void CreateScheduleFor(BackupDatabaseInfo databaseInfo)
        {
            try
            {
                DateTime currentTime = _sharedTimeZone.Now;
                BackupSchedule saveObj = new BackupSchedule
                {
                    BackupDatabaseInfoId = databaseInfo.Id,
                    ActiveDirectoryId = databaseInfo.ActiveDirectoryId,
                    ScheduleType = BackupScheduleType.FULLBACKUP.ToString(),
                    EveryHours = 24,
                    StartDate = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day + 1),
                    CreatedOn = currentTime,
                    Name = databaseInfo.Name,
                    LastRun = null
                };
                bool savedSuccess = _schedulePersistanceService.AddOrUpdate(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to Auto Create Daily Backup for Database Key: {databaseInfo.Id},Error: {ex.Message}"); }
        }

        [HttpPut("{id}")]
        public ActionResult Put([FromBody] BackupDatabaseRequest request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var savedObj = _backupDatabasePersistanceService.GetById(id);
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
                savedObj.BackupExpiryAgeInDays = request.BackupExpiryAgeInDays;

                bool updatedSuccess = _backupDatabasePersistanceService.Update(savedObj);
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
                bool removedSuccess = _backupDatabasePersistanceService.Remove(id);
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
        public async Task<IEnumerable<string>> GetPreGetDbCollection([FromForm] DatabaseCollectionRequest request)
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
