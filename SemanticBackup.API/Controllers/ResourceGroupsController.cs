﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private readonly IContentDeliveryConfigPersistanceService _contentDeliveryConfigPersistanceService;
        private readonly PersistanceOptions _persistanceOptions;

        public ResourceGroupsController(ILogger<ResourceGroupsController> logger, IResourceGroupPersistanceService resourceGroupPersistance, IContentDeliveryConfigPersistanceService contentDeliveryConfigPersistanceService, PersistanceOptions persistanceOptions)
        {
            _logger = logger;
            this._activeResourcegroupService = resourceGroupPersistance;
            this._contentDeliveryConfigPersistanceService = contentDeliveryConfigPersistanceService;
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
                request.MaximumRunningBots = (request.MaximumRunningBots < 1) ? 1 : request.MaximumRunningBots;
                //Proceed
                long.TryParse(DateTime.UtcNow.ToString("yyyyMMddHHmmss"), out long lastAccess);
                ResourceGroup saveObj = new ResourceGroup
                {
                    Name = request.Name,
                    LastAccess = lastAccess,
                    TimeZone = request.TimeZone,
                    MaximumRunningBots = request.MaximumRunningBots,
                    CompressBackupFiles = request.CompressBackupFiles,
                    BackupExpiryAgeInDays = request.BackupExpiryAgeInDays,
                };
                bool savedSuccess = _activeResourcegroupService.Add(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");

                //Post Check and Get Delivery Settings
                List<ContentDeliveryConfiguration> configs = GetPostedConfigurations(saveObj.Id, request);
                if (configs != null && configs.Count > 0)
                {
                    bool addedSuccess = this._contentDeliveryConfigPersistanceService.AddOrUpdate(configs);
                    if (!addedSuccess)
                        _logger.LogWarning("Resource Group Content Delivery Settings were not Saved");
                }
                return Ok(savedSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public ActionResult Put([FromBody] ResourceGroupRequest request, string id)
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
                savedObj.MaximumRunningBots = request.MaximumRunningBots;
                savedObj.CompressBackupFiles = request.CompressBackupFiles;
                savedObj.BackupExpiryAgeInDays = request.BackupExpiryAgeInDays;
                //Update
                bool updatedSuccess = _activeResourcegroupService.Update(savedObj);
                if (!updatedSuccess)
                    throw new Exception("Data was not Updated");
                //Post Check and Get Delivery Settings
                List<ContentDeliveryConfiguration> configs = GetPostedConfigurations(savedObj.Id, request);
                if (configs != null && configs.Count > 0)
                {
                    //Remove Old
                    this._contentDeliveryConfigPersistanceService.RemoveAllByResourceGroup(savedObj.Id);
                    //Update New
                    bool addedSuccess = this._contentDeliveryConfigPersistanceService.AddOrUpdate(configs);
                    if (!addedSuccess)
                        _logger.LogWarning("Resource Group Content Delivery Settings were not Updated");
                }
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

        private List<ContentDeliveryConfiguration> GetPostedConfigurations(string resourceGroupId, ResourceGroupRequest request)
        {
            List<ContentDeliveryConfiguration> configs = new List<ContentDeliveryConfiguration>();
            try
            {
                if (request == null)
                    return configs;
                //Download Link
                if (request.RSDownloadLinkSetting != null && request.RSDownloadLinkSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.DIRECT_LINK.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSDownloadLinkSetting),
                        PriorityIndex = 0
                    });
                //FTP Configs
                if (request.RSFTPSetting != null && request.RSFTPSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.FTP_UPLOAD.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSFTPSetting),
                        PriorityIndex = 1
                    });
                //Email SMTP
                if (request.RSEmailSMTPSetting != null && request.RSEmailSMTPSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.EMAIL_SMTP.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSEmailSMTPSetting),
                        PriorityIndex = 2
                    });
                //Mega Storage
                if (request.RSMegaNxSetting != null && request.RSMegaNxSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.MEGA_STORAGE.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSMegaNxSetting),
                        PriorityIndex = 3
                    });

                //Dropbox
                if (request.RSDropBoxSetting != null && request.RSDropBoxSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.DROPBOX.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSDropBoxSetting),
                        PriorityIndex = 4
                    });
                //Azure Blob Storage
                if (request.RSAzureBlobStorageSetting != null && request.RSAzureBlobStorageSetting.IsEnabled)
                    configs.Add(new ContentDeliveryConfiguration
                    {
                        IsEnabled = true,
                        DeliveryType = ContentDeliveryType.AZURE_BLOB_STORAGE.ToString(),
                        ResourceGroupId = resourceGroupId,
                        Configuration = JsonConvert.SerializeObject(request.RSAzureBlobStorageSetting),
                        PriorityIndex = 5
                    });
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
            return configs;
        }
    }
}
