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
    [Route("{resourcegroup}/api/[controller]")]
    public class ResourceGroupsController : ControllerBase
    {
        private readonly ILogger<ResourceGroupsController> _logger;
        private readonly IResourceGroupPersistanceService _activeResourcegroupService;
        private readonly SharedTimeZone _sharedTimeZone;

        public ResourceGroupsController(ILogger<ResourceGroupsController> logger, IResourceGroupPersistanceService resourceGroupPersistance, SharedTimeZone sharedTimeZone)
        {
            _logger = logger;
            this._activeResourcegroupService = resourceGroupPersistance;
            this._sharedTimeZone = sharedTimeZone;
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
                DateTime currentTime = _sharedTimeZone.Now;
                long.TryParse(DateTime.Now.ToString("yyyyMMddHHmmss"), out long lastAccess);
                ResourceGroup saveObj = new ResourceGroup
                {
                    Name = request.Name,
                    LastAccess = lastAccess
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
                savedObj.Name = request.Name;
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
