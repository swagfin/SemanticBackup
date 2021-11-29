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
    public class BackupDatabasesController : ControllerBase
    {
        private readonly ILogger<BackupDatabasesController> _logger;
        private readonly IDatabaseInfoPersistanceService _backupDatabasePersistanceService;
        private readonly SharedTimeZone _sharedTimeZone;

        public BackupDatabasesController(ILogger<BackupDatabasesController> logger, IDatabaseInfoPersistanceService databaseInfoPersistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._backupDatabasePersistanceService = databaseInfoPersistanceService;
            this._sharedTimeZone = sharedTimeZone;
        }

        [HttpGet]
        public ActionResult<List<BackupDatabaseInfo>> Get()
        {
            try
            {
                return _backupDatabasePersistanceService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupDatabaseInfo>();
            }
        }
        [HttpPost]
        public ActionResult Post([FromBody] BackupDatabaseRequest request)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                DateTime currentTime = _sharedTimeZone.Now;
                BackupDatabaseInfo saveObj = new BackupDatabaseInfo
                {
                    Server = request.Server,
                    DatabaseName = request.DatabaseName,
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
                return Ok(saveObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
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
    }
}
