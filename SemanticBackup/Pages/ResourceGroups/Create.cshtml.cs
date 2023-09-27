using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using SemanticBackup.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        private readonly IContentDeliveryConfigRepository _contentDeliveryConfigRepository;

        public string ErrorResponse { get; set; } = null;
        public List<string> TimeZoneCollections { get; }
        [BindProperty]
        public ResourceGroupRequest request { get; set; }

        public CreateModel(ILogger<IndexModel> logger, IOptions<SystemConfigOptions> options, IResourceGroupRepository resourceGroupPersistance, IContentDeliveryConfigRepository contentDeliveryConfigRepository, TimeZoneHelper timeZoneHelper)
        {
            this._logger = logger;
            this._persistanceOptions = options.Value;
            this._resourceGroupPersistance = resourceGroupPersistance;
            this._contentDeliveryConfigRepository = contentDeliveryConfigRepository;
            this.TimeZoneCollections = timeZoneHelper.GetAll();
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (request == null)
                    return Page();
                //Validate Fields
                if (!IsValidationPassed())
                    return Page();
                //Check TimeZone Provided
                request.TimeZone = (string.IsNullOrWhiteSpace(request.TimeZone)) ? _persistanceOptions.ServerDefaultTimeZone : request.TimeZone;
                request.MaximumRunningBots = (request.MaximumRunningBots < 1) ? 1 : request.MaximumRunningBots;
                //Proceed
                ResourceGroup saveObj = new ResourceGroup
                {
                    Name = request.Name,
                    LastAccess = DateTime.UtcNow.ConvertLongFormat(),
                    TimeZone = request.TimeZone,
                    MaximumRunningBots = request.MaximumRunningBots,
                    CompressBackupFiles = request.CompressBackupFiles,
                    BackupExpiryAgeInDays = request.BackupExpiryAgeInDays,
                    NotifyEmailDestinations = request.NotifyEmailDestinations,
                    NotifyOnErrorBackupDelivery = request.NotifyOnErrorBackupDelivery,
                    NotifyOnErrorBackups = request.NotifyOnErrorBackups,
                };
                bool savedSuccess = await _resourceGroupPersistance.AddAsync(saveObj);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");

                //Post Check and Get Delivery Settings
                List<ContentDeliveryConfiguration> configs = GetPostedConfigurations(saveObj.Id, request);
                if (configs != null && configs.Count > 0)
                {
                    bool addedSuccess = await this._contentDeliveryConfigRepository.AddOrUpdateAsync(configs);
                    if (!addedSuccess)
                        _logger.LogWarning("Resource Group Content Delivery Settings were not Saved");
                }
                return Redirect("/resource-groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorResponse = ex.Message;
                return Page();
            }

        }

        private bool IsValidationPassed()
        {
            try
            {
                //Check Supported Timezone
                if (string.IsNullOrEmpty(request.TimeZone) || !this.TimeZoneCollections.Contains(request.TimeZone))
                {
                    ErrorResponse = "Enter a valid Timezone from the List";
                    return false;
                }
                if (request.RSFTPSetting != null && request.RSFTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(request.RSFTPSetting.Password) || string.IsNullOrEmpty(request.RSFTPSetting.Username) || string.IsNullOrEmpty(request.RSFTPSetting.Server))
                    {
                        ErrorResponse = "Server, Username and Password Fields are required for[ FTP Content Delivery] is been Enabled";
                        return false;
                    }
                if (request.RSEmailSMTPSetting != null && request.RSEmailSMTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(request.RSEmailSMTPSetting.SMTPHost) || string.IsNullOrEmpty(request.RSEmailSMTPSetting.SMTPEmailAddress) || string.IsNullOrEmpty(request.RSEmailSMTPSetting.SMTPEmailCredentials))
                    {
                        ErrorResponse = "SMTP Host, SMTP Email Address and SMTP Email Credentials Fields are required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }
                    else if (string.IsNullOrWhiteSpace(request.RSEmailSMTPSetting.SMTPDestinations))
                    {
                        ErrorResponse = "SMTP Host Destination Address have not been added, at list one destination address required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }

                if (request.RSDropBoxSetting != null && request.RSDropBoxSetting.IsEnabled)
                    if (string.IsNullOrEmpty(request.RSDropBoxSetting.AccessToken) || string.IsNullOrEmpty(request.RSDropBoxSetting.Directory))
                    {
                        ErrorResponse = "Dropbox API Token and Directory field are required If [Dropbox Content Delivery] has been Enabled";
                        return false;
                    }
                    else
                    if (request.RSDropBoxSetting.AccessToken.Length < 16)
                    {
                        ErrorResponse = "Dropbox API Token provided is Invalid";
                        return false;
                    }
                if (request.RSAzureBlobStorageSetting != null && request.RSAzureBlobStorageSetting.IsEnabled)
                    if (string.IsNullOrEmpty(request.RSAzureBlobStorageSetting.ConnectionString) || string.IsNullOrEmpty(request.RSAzureBlobStorageSetting.BlobContainer))
                    {
                        ErrorResponse = "Azure Blob Storage Connection String and Blob Container fields are required If [Azure Blob Storage Content Delivery] has been Enabled";
                        return false;
                    }
                //Notifications
                if (request.NotifyOnErrorBackups || request.NotifyOnErrorBackupDelivery)
                    if (string.IsNullOrWhiteSpace(request.NotifyEmailDestinations))
                    {
                        ErrorResponse = "Notification Address must be set if [Notification of Execution Run Failure] is Enabled";
                        return false;
                    }
                return true;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); ErrorResponse = ex.Message; return false; }

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
