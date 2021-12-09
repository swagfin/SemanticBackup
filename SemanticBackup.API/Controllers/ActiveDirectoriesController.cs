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
    [Route("{directory}/api/[controller]")]
    public class ActiveDirectoriesController : ControllerBase
    {
        private readonly ILogger<ActiveDirectoriesController> _logger;
        private readonly IActiveDirectoryPersistanceService _activeDirectoryService;
        private readonly SharedTimeZone _sharedTimeZone;

        public ActiveDirectoriesController(ILogger<ActiveDirectoriesController> logger, IActiveDirectoryPersistanceService persistanceService, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._activeDirectoryService = persistanceService;
            this._sharedTimeZone = sharedTimeZone;
        }
        [HttpGet]
        public ActionResult<List<ActiveDirectory>> GetDirectories()
        {
            try
            {
                return _activeDirectoryService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ActiveDirectory>();
            }
        }
        [HttpGet("{id}")]
        public ActionResult<ActiveDirectory> Get(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var record = _activeDirectoryService.GetById(id);
                if (record == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] ActiveDirectoryRequest request)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                DateTime currentTime = _sharedTimeZone.Now;
                long.TryParse(DateTime.Now.ToString("yyyyMMddHHmmss"), out long lastAccess);
                ActiveDirectory saveObj = new ActiveDirectory
                {
                    Name = request.Name,
                    LastAccess = lastAccess
                };
                bool savedSuccess = _activeDirectoryService.Add(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
                return Ok(savedSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public ActionResult Put([FromBody] ActiveDirectory request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Verify Database Info Exists
                //Proceed
                var savedObj = _activeDirectoryService.GetById(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Update Params
                savedObj.Name = request.Name;
                bool updatedSuccess = _activeDirectoryService.Update(savedObj);
                if (!updatedSuccess)
                    throw new Exception("Data was not Updated");
                return Ok(updatedSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
        [HttpGet("switch-directory/{id}")]
        public ActionResult GetSwitchDirectory(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                bool updatedSuccess = _activeDirectoryService.Switch(id);
                if (!updatedSuccess)
                    throw new Exception("Data was not Updated");
                return Ok(updatedSuccess);
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
                bool removedSuccess = _activeDirectoryService.Remove(id);
                if (!removedSuccess)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                else
                    return Ok(removedSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
