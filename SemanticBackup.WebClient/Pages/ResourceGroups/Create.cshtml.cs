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
                //Proceed
                return true;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); ErrorResponse = ex.Message; return false; }

        }
    }
}
