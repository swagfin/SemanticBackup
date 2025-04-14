using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ILogger<EditModel> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        private readonly IBackupProviderForMySQLServer _backupProviderForMySQLServer;
        private readonly IBackupProviderForSQLServer _backupProviderForSQLServer;

        public string ErrorResponse { get; set; } = null;
        [BindProperty]
        public ResourceGroup ResourceGrp { get; set; } = new ResourceGroup();

        public EditModel(ILogger<EditModel> logger, SystemConfigOptions options, IResourceGroupRepository resourceGroupPersistance, IBackupProviderForMySQLServer backupProviderForMySQLServer, IBackupProviderForSQLServer backupProviderForSQLServer)
        {
            _logger = logger;
            _persistanceOptions = options;
            _resourceGroupPersistance = resourceGroupPersistance;
            _backupProviderForMySQLServer = backupProviderForMySQLServer;
            _backupProviderForSQLServer = backupProviderForSQLServer;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                ResourceGrp = await _resourceGroupPersistance.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect("/resource-groups");
            }
        }

        public async Task<IActionResult> OnPostAsync(string resourceGroupId)
        {
            try
            {
                ResourceGroup existingResourceGrp = await _resourceGroupPersistance.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                //proceed
                if (ResourceGrp == null)
                    return Page();
                //Validate Fields
                if (!IsValidationPassed())
                    return Page();
                //proceed
                //proceed
                ResourceGrp.MaximumRunningBots = (ResourceGrp.MaximumRunningBots < 1) ? 1 : ResourceGrp.MaximumRunningBots;
                //atttempt check connection string
                await ValidateDbConnectionAsync(ResourceGrp);
                //update details
                ResourceGrp.Id = existingResourceGrp.Id;
                ResourceGrp.Name = existingResourceGrp.Name;
                bool savedSuccess = await _resourceGroupPersistance.UpdateAsync(ResourceGrp);
                if (!savedSuccess)
                    throw new Exception("Data was not Saved");
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
                if (string.IsNullOrWhiteSpace(ResourceGrp.DbServer))
                {
                    ErrorResponse = "Database Server Name was not provided";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(ResourceGrp.DbUsername))
                {
                    ErrorResponse = "Database Username is required";
                    return false;
                }
                //check delivery config
                ResourceGrp.BackupDeliveryConfig ??= new BackupDeliveryConfig();

                if (ResourceGrp.BackupDeliveryConfig.Ftp != null && ResourceGrp.BackupDeliveryConfig.Ftp.IsEnabled)
                    if (string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Ftp.Password) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Ftp.Username) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Ftp.Server))
                    {
                        ErrorResponse = "Server, Username and Password Fields are required for[ FTP Content Delivery] is been Enabled";
                        return false;
                    }
                if (ResourceGrp.BackupDeliveryConfig.Smtp != null && ResourceGrp.BackupDeliveryConfig.Smtp.IsEnabled)
                    if (string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Smtp.SMTPHost) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Smtp.SMTPEmailAddress) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Smtp.SMTPEmailCredentials))
                    {
                        ErrorResponse = "SMTP Host, SMTP Email Address and SMTP Email Credentials Fields are required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }
                    else if (string.IsNullOrWhiteSpace(ResourceGrp.BackupDeliveryConfig.Smtp.SMTPDestinations))
                    {
                        ErrorResponse = "SMTP Host Destination Address have not been added, at list one destination address required If [Email SMTP Content Delivery] has been Enabled";
                        return false;
                    }

                if (ResourceGrp.BackupDeliveryConfig.Dropbox != null && ResourceGrp.BackupDeliveryConfig.Dropbox.IsEnabled)
                    if (string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Dropbox.AccessToken) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.Dropbox.Directory))
                    {
                        ErrorResponse = "Dropbox API Token and Directory field are required If [Dropbox Content Delivery] has been Enabled";
                        return false;
                    }
                    else
                    if (ResourceGrp.BackupDeliveryConfig.Dropbox.AccessToken.Length < 16)
                    {
                        ErrorResponse = "Dropbox API Token provided is Invalid";
                        return false;
                    }

                if (ResourceGrp.BackupDeliveryConfig.AzureBlobStorage != null && ResourceGrp.BackupDeliveryConfig.AzureBlobStorage.IsEnabled)
                    if (string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.AzureBlobStorage.ConnectionString) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.AzureBlobStorage.BlobContainer))
                    {
                        ErrorResponse = "Azure Blob Storage Connection String and Blob Container fields are required If [Azure Blob Storage Content Delivery] has been Enabled";
                        return false;
                    }

                if (ResourceGrp.BackupDeliveryConfig.ObjectStorage != null && ResourceGrp.BackupDeliveryConfig.ObjectStorage.IsEnabled)
                    if (string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.ObjectStorage.Server) || string.IsNullOrEmpty(ResourceGrp.BackupDeliveryConfig.ObjectStorage.Bucket))
                    {
                        ErrorResponse = "Object Storage Server and Bucket fields are required If [Object Storage Content Delivery] has been Enabled";
                        return false;
                    }

                //Notifications
                if (ResourceGrp.NotifyOnErrorBackups || ResourceGrp.NotifyOnErrorBackupDelivery)
                    if (string.IsNullOrWhiteSpace(ResourceGrp.NotifyEmailDestinations))
                    {
                        ErrorResponse = "Notification Address must be set if [Notification of Execution Run Failure] is Enabled";
                        return false;
                    }
                return true;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); ErrorResponse = ex.Message; return false; }

        }
        private async Task ValidateDbConnectionAsync(ResourceGroup saveObj)
        {
            //Finnally Validate Database Connection
            if (saveObj.DbType.Contains("SQLSERVER"))
            {
                (bool success, string err) = await _backupProviderForSQLServer.TryTestDbConnectivityAsync(saveObj);
                if (!success)
                    throw new Exception(err);
            }
            else if (ResourceGrp.DbType.Contains("MYSQL") || ResourceGrp.DbType.Contains("MARIADB"))
            {
                (bool success, string err) = await _backupProviderForMySQLServer.TryTestDbConnectivityAsync(saveObj);
                if (!success)
                    throw new Exception(err);
            }
        }
    }
}
