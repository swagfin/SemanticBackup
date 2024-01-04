using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly SystemConfigOptions _persistanceOptions;
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        private readonly IBackupProviderForMySQLServer _backupProviderForMySQLServer;
        private readonly IBackupProviderForSQLServer _backupProviderForSQLServer;

        public string ErrorResponse { get; set; } = null;
        [BindProperty]
        public ResourceGroupRequest request { get; set; }

        public CreateModel(ILogger<IndexModel> logger, SystemConfigOptions options, IResourceGroupRepository resourceGroupPersistance, IBackupProviderForMySQLServer backupProviderForMySQLServer, IBackupProviderForSQLServer backupProviderForSQLServer)
        {
            this._logger = logger;
            this._persistanceOptions = options;
            this._resourceGroupPersistance = resourceGroupPersistance;
            this._backupProviderForMySQLServer = backupProviderForMySQLServer;
            this._backupProviderForSQLServer = backupProviderForSQLServer;
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
                request.MaximumRunningBots = (request.MaximumRunningBots < 1) ? 1 : request.MaximumRunningBots;
                //Proceed
                ResourceGroup saveObj = new ResourceGroup
                {
                    Name = request.Name,
                    MaximumRunningBots = request.MaximumRunningBots,
                    DbType = request.DbType,
                    DbServer = request.DbServer,
                    DbPort = request.DbPort,
                    DbPassword = request.DbPassword,
                    DbUsername = request.DbUsername,
                    CompressBackupFiles = request.CompressBackupFiles,
                    BackupExpiryAgeInDays = request.BackupExpiryAgeInDays,
                    NotifyEmailDestinations = request.NotifyEmailDestinations,
                    NotifyOnErrorBackupDelivery = request.NotifyOnErrorBackupDelivery,
                    NotifyOnErrorBackups = request.NotifyOnErrorBackups,
                    LastAccess = DateTime.UtcNow.Ticks,
                    BackupDeliveryConfig = new BackupDeliveryConfig
                    {
                        DownloadLink = new DownloadLinkDeliveryConfig
                        {
                            IsEnabled = request.RSDownloadLinkSetting.IsEnabled,
                            DownloadLinkType = request.RSDownloadLinkSetting.DownloadLinkType
                        },
                        Ftp = new FtpDeliveryConfig
                        {
                            IsEnabled = request.RSFTPSetting.IsEnabled,
                            Server = request.RSFTPSetting.Server,
                            Username = request.RSFTPSetting.Username,
                            Password = request.RSFTPSetting.Password,
                            Directory = request.RSFTPSetting.Directory
                        },
                        Smtp = new SmtpDeliveryConfig
                        {
                            IsEnabled = request.RSEmailSMTPSetting.IsEnabled,
                            SMTPEnableSSL = request.RSEmailSMTPSetting.SMTPEnableSSL,
                            SMTPHost = request.RSEmailSMTPSetting.SMTPHost,
                            SMTPPort = request.RSEmailSMTPSetting.SMTPPort,
                            SMTPEmailAddress = request.RSEmailSMTPSetting.SMTPEmailAddress,
                            SMTPEmailCredentials = request.RSEmailSMTPSetting.SMTPEmailCredentials,
                            SMTPDefaultSMTPFromName = request.RSEmailSMTPSetting.SMTPDefaultSMTPFromName,
                            SMTPDestinations = request.RSEmailSMTPSetting.SMTPDestinations
                        },
                        Dropbox = new DropboxDeliveryConfig
                        {
                            IsEnabled = request.RSDropBoxSetting.IsEnabled,
                            AccessToken = request.RSDropBoxSetting.AccessToken,
                            Directory = request.RSDropBoxSetting.Directory
                        },
                        AzureBlobStorage = new AzureBlobStorageDeliveryConfig
                        {
                            IsEnabled = request.RSAzureBlobStorageSetting.IsEnabled,
                            BlobContainer = request.RSAzureBlobStorageSetting.BlobContainer,
                            ConnectionString = request.RSAzureBlobStorageSetting.ConnectionString
                        }
                    }
                };

                //atttempt check connection string
                await ValidateDbConnectionAsync(saveObj);

                bool savedSuccess = await _resourceGroupPersistance.AddAsync(saveObj);
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
                if (string.IsNullOrWhiteSpace(request.DbServer))
                {
                    ErrorResponse = "Database Server Name was not provided";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(request.DbUsername))
                {
                    ErrorResponse = "Database Username is required";
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
        private async Task ValidateDbConnectionAsync(ResourceGroup saveObj)
        {
            //Finnally Validate Database Connection
            if (saveObj.DbType.Contains("SQLSERVER"))
            {
                var response = await _backupProviderForSQLServer.TryTestDbConnectivityAsync(saveObj);
                if (!response.success)
                    throw new Exception(response.err);
            }
            else if (request.DbType.Contains("MYSQL") || request.DbType.Contains("MARIADB"))
            {
                var response = await _backupProviderForMySQLServer.TryTestDbConnectivityAsync(saveObj);
                if (!response.success)
                    throw new Exception(response.err);
            }
        }
    }
}
