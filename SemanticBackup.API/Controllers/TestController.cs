using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Core;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;

namespace SemanticBackup.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly SharedTimeZone _serverTimeZone;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;
        private readonly IDatabaseInfoPersistanceService _databaseInfoPersistanceService;
        private readonly PersistanceOptions _persistanceOptions;

        public TestController(ILogger<TestController> logger, SharedTimeZone sharedTimeZone,
            IBackupSchedulePersistanceService backupSchedulePersistanceService,
            IDatabaseInfoPersistanceService databaseInfoPersistanceService,
            PersistanceOptions persistanceOptions
            )
        {
            this._logger = logger;
            this._serverTimeZone = sharedTimeZone;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
            this._databaseInfoPersistanceService = databaseInfoPersistanceService;
            this._persistanceOptions = persistanceOptions;
        }
        [HttpGet, HttpPost]
        public ActionResult<string> GetCreateSmapleTest()
        {
            try
            {
                //Proceed
                string _dbkey = "44405576-A24F-496E-BC08-A1C4D8A48F45";
                string _schedulekey = "3755D701-5B3B-473C-83FC-D5DCEC179079";

                //Try Remove
                _backupSchedulePersistanceService.Remove(_schedulekey);
                _databaseInfoPersistanceService.Remove(_dbkey);

                int expiryDays = _persistanceOptions.DefaultBackupExpiryAgeInDays;
                DateTime currentTime = _serverTimeZone.Now;
                BackupDatabaseInfo defaultDb = new BackupDatabaseInfo
                {
                    Id = _dbkey,
                    DatabaseName = "test",
                    Server = "127.0.0.1",
                    Username = "sa",
                    Password = "12345678",
                    DatabaseType = BackupDatabaseInfoDbType.SQLSERVER2019.ToString(),
                    Port = 1433,
                    Description = "Testing Backup Database",
                    BackupExpiryAgeInDays = expiryDays,
                    DateRegistered = currentTime
                };

                bool dbSaved = _databaseInfoPersistanceService.AddOrUpdate(defaultDb);
                //Proceed
                BackupSchedule backupSchedule = new BackupSchedule
                {
                    Id = _schedulekey,
                    Name = defaultDb.Name,
                    BackupDatabaseInfoId = _dbkey,
                    StartDate = currentTime,
                    LastRun = currentTime.AddDays(-2),
                    EveryHours = 24,
                    ScheduleType = BackupScheduleType.FULLBACKUP.ToString(),
                };
                bool scheduleSaved = _backupSchedulePersistanceService.AddOrUpdate(backupSchedule);

                return Ok($"[DBINFO-SAVED]: {dbSaved} | [SCHEDULE-SAVED]: {scheduleSaved}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
