using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("{resourcegroup}/api/[controller]")]
    public class ResourceGroupsController : ControllerBase
    {
        private readonly ILogger<ResourceGroupsController> _logger;
        private readonly IResourceGroupPersistanceService _activeResourcegroupService;
        private readonly PersistanceOptions _persistanceOptions;

        public ResourceGroupsController(ILogger<ResourceGroupsController> logger, IResourceGroupPersistanceService resourceGroupPersistance, PersistanceOptions persistanceOptions)
        {
            _logger = logger;
            this._activeResourcegroupService = resourceGroupPersistance;
            this._persistanceOptions = persistanceOptions;
        }
        [HttpGet]
        public ActionResult<List<ResourceGroup>> GetDirectories()
        {
            try
            {
                return _activeResourcegroupService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<ResourceGroup>();
            }
        }
        [HttpGet("{id}")]
        public ActionResult<ResourceGroup> Get(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                var record = _activeResourcegroupService.GetById(id);
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
        public ActionResult Post([FromBody] ResourceGroupRequest request)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                //Check TimeZone Provided
                request.TimeZone = (string.IsNullOrWhiteSpace(request.TimeZone)) ? _persistanceOptions.ServerDefaultTimeZone : request.TimeZone;
                request.MaximumBackupRunningThreads = (request.MaximumBackupRunningThreads < 1) ? 1 : request.MaximumBackupRunningThreads;
                //Proceed
                long.TryParse(DateTime.UtcNow.ToString("yyyyMMddHHmmss"), out long lastAccess);
                ResourceGroup saveObj = new ResourceGroup
                {
                    Name = request.Name,
                    LastAccess = lastAccess,
                    TimeZone = request.TimeZone,
                    MaximumBackupRunningThreads = request.MaximumBackupRunningThreads
                };
                bool savedSuccess = _activeResourcegroupService.Add(saveObj);
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
        public ActionResult Put([FromBody] ResourceGroup request, string id)
        {
            try
            {
                if (request == null)
                    throw new Exception("Object value can't be NULL");
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                //Verify Database Info Exists
                //Proceed
                var savedObj = _activeResourcegroupService.GetById(id);
                if (savedObj == null)
                    return new NotFoundObjectResult($"No Data Found with Key: {id}");
                //Update Params
                request.TimeZone = (string.IsNullOrWhiteSpace(request.TimeZone)) ? _persistanceOptions.ServerDefaultTimeZone : request.TimeZone;
                savedObj.Name = request.Name;
                savedObj.TimeZone = request.TimeZone;
                //Update
                bool updatedSuccess = _activeResourcegroupService.Update(savedObj);
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
        [HttpGet("switch-resource-group/{id}")]
        public ActionResult GetSwitchResourceGroup(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Id can't be NULL");
                bool updatedSuccess = _activeResourcegroupService.Switch(id);
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
                bool removedSuccess = _activeResourcegroupService.Remove(id);
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
