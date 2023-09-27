using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
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
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        public string ErrorResponse { get; set; } = null;
        public List<string> TimeZoneCollections { get; }
        [BindProperty]
        public ResourceGroupRequest RGRequest { get; set; }

        public CreateModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupPersistance, TimeZoneHelper timeZoneHelper)
        {
            this._logger = logger;
            this._resourceGroupPersistance = resourceGroupPersistance;
            this.TimeZoneCollections = timeZoneHelper.GetAll();
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (RGRequest == null)
                    return Page();
                //Validate Fields
                if (!IsValidationPassed())
                    return Page();
                //Checks Pattern
                bool addedSuccess = await this._resourceGroupService.AddAsync(RGRequest);
                if (addedSuccess)
                    return Redirect("/resource-groups/");
                ErrorResponse = "Unable to Save Resource Group, Please Try Again Later";
                return Page();
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
                if (string.IsNullOrEmpty(RGRequest.TimeZone) || !this.TimeZoneCollections.Contains(RGRequest.TimeZone))
                {
                    ErrorResponse = "Enter a valid Timezone from the List";
                    return false;
                }
                if (RGRequest.RSFTPSetting != null && RGRequest.RSFTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSFTPSetting.Password) || string.IsNullOrEmpty(RGRequest.RSFTPSetting.Username) || string.IsNullOrEmpty(RGRequest.RSFTPSetting.Server))
                    {
                        ErrorResponse = "Server, Username and Password Fields are required for[ FTP Content Delivery] is been Enabled";
                        return false;
                    }
                if (RGRequest.RSEmailSMTPSetting != null && RGRequest.RSEmailSMTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPHost) || string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPEmailAddress) || string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPEmailCredentials))
                    {
                        ErrorResponse = "SMTP Host, SMTP Email Address and SMTP Email Credentials Fields are required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }
                    else if (string.IsNullOrWhiteSpace(RGRequest.RSEmailSMTPSetting.SMTPDestinations))
                    {
                        ErrorResponse = "SMTP Host Destination Address have not been added, at list one destination address required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }

                if (RGRequest.RSDropBoxSetting != null && RGRequest.RSDropBoxSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSDropBoxSetting.AccessToken) || string.IsNullOrEmpty(RGRequest.RSDropBoxSetting.Directory))
                    {
                        ErrorResponse = "Dropbox API Token and Directory field are required If [Dropbox Content Delivery] has been Enabled";
                        return false;
                    }
                    else
                    if (RGRequest.RSDropBoxSetting.AccessToken.Length < 16)
                    {
                        ErrorResponse = "Dropbox API Token provided is Invalid";
                        return false;
                    }
                if (RGRequest.RSAzureBlobStorageSetting != null && RGRequest.RSAzureBlobStorageSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSAzureBlobStorageSetting.ConnectionString) || string.IsNullOrEmpty(RGRequest.RSAzureBlobStorageSetting.BlobContainer))
                    {
                        ErrorResponse = "Azure Blob Storage Connection String and Blob Container fields are required If [Azure Blob Storage Content Delivery] has been Enabled";
                        return false;
                    }
                //Notifications
                if (RGRequest.NotifyOnErrorBackups || RGRequest.NotifyOnErrorBackupDelivery)
                    if (string.IsNullOrWhiteSpace(RGRequest.NotifyEmailDestinations))
                    {
                        ErrorResponse = "Notification Address must be set if [Notification of Execution Run Failure] is Enabled";
                        return false;
                    }
                return true;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); ErrorResponse = ex.Message; return false; }

        }
    }
}
