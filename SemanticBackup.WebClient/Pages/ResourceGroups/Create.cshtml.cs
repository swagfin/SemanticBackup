using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.ResourceGroups
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupService _resourceGroupService;
        public string ErrorResponse { get; set; } = null;
        public List<TimeZoneRecord> TimeZoneCollections { get; }
        [BindProperty]
        public ResourceGroupRequest RGRequest { get; set; }

        public CreateModel(ILogger<IndexModel> logger, IResourceGroupService resourceGroupService, TimeZoneHelper timeZoneHelper)
        {
            this._logger = logger;
            this._resourceGroupService = resourceGroupService;
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
                if (RGRequest.RSFTPSetting != null && RGRequest.RSFTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSFTPSetting.Password) || string.IsNullOrEmpty(RGRequest.RSFTPSetting.Username) || string.IsNullOrEmpty(RGRequest.RSFTPSetting.Server))
                    {
                        ErrorResponse = "Server, Username and Password Fields are required for FTP Content Delivery is been Enabled";
                        return false;
                    }
                if (RGRequest.RSEmailSMTPSetting != null && RGRequest.RSEmailSMTPSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPHost) || string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPEmailAddress) || string.IsNullOrEmpty(RGRequest.RSEmailSMTPSetting.SMTPEmailCredentials))
                    {
                        ErrorResponse = "SMTP Host, SMTP Email Address and SMTP Email Credentials Fields are required If Email SMTP Content Delivery has been Enabled";
                        return false;
                    }
                    else if (string.IsNullOrWhiteSpace(RGRequest.RSEmailSMTPSetting.SMTPDestinations))
                    {
                        ErrorResponse = "SMTP Host Destination Address have not been added, at list one destination address required If Email SMTP Content Delivery has been Enabled";
                        return false;
                    }

                if (RGRequest.RSDropBoxSetting != null && RGRequest.RSDropBoxSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSDropBoxSetting.AccessToken) || string.IsNullOrEmpty(RGRequest.RSDropBoxSetting.Directory))
                    {
                        ErrorResponse = "Dropbox API Token and Directory field are required If Dropbox Content Delivery has been Enabled";
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
                        ErrorResponse = "Azure Blob Storage Connection String and Blob Container fields are required If Azure Blob Storage Content Delivery has been Enabled";
                        return false;
                    }
                if (RGRequest.RSMegaNxSetting != null && RGRequest.RSMegaNxSetting.IsEnabled)
                    if (string.IsNullOrEmpty(RGRequest.RSMegaNxSetting.Username) || string.IsNullOrEmpty(RGRequest.RSMegaNxSetting.Password))
                    {
                        ErrorResponse = "Mega Storage Username and Password fields are required If Mega Storage Nz Content Delivery has been Enabled";
                        return false;
                    }
                return true;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); ErrorResponse = ex.Message; return false; }

        }
    }
}
