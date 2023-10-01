using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SemanticBackup.Services
{
    public class StatusNotificationService : IRecordStatusChangedNotifier
    {
        private readonly ILogger<StatusNotificationService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SystemConfigOptions _options;
        public StatusNotificationService(ILogger<StatusNotificationService> logger, SystemConfigOptions options, IServiceScopeFactory scopeFactory)
        {
            this._logger = logger;
            this._scopeFactory = scopeFactory;
            this._options = options;
        }

        public void DispatchBackupRecordUpdatedStatus(BackupRecord backupRecord, bool isNewRecord = false)
        {
            _logger.LogInformation("Received BackupRecordUpdated Notification....");
            _ = SendBackupRecordEmailNotificationAsync(backupRecord);
        }

        public void DispatchContentDeliveryUpdatedStatus(ContentDeliveryRecord record, bool isNewRecord = false)
        {
            _logger.LogInformation("Received ContentDeliveryUpdate Notification....");
            _ = SendContentDeliveryNotificationAsync(record);
        }

        private async Task SendBackupRecordEmailNotificationAsync(BackupRecord backupRecord)
        {
            try
            {
                _logger.LogInformation("Received BackupRecordUpdated Notification....");
                if (backupRecord.BackupStatus != BackupRecordBackupStatus.ERROR.ToString())
                    return;
                using (var scope = _scopeFactory.CreateScope())
                {
                    IResourceGroupRepository _resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                    //Info On Resource Group
                    ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdOrKeyAsync(backupRecord.ResourceGroupId);
                    if (resourceGroup == null)
                        return; //No Valid Resource Group
                    if (!resourceGroup.NotifyOnErrorBackups)
                        return; //Disabled
                    string subject = $"[{resourceGroup.Name}/{backupRecord.Name}] Database Backup Failed";
                    string emailBody = $"<h3>[{resourceGroup.Name}/{backupRecord.Name}] Backup Run</h3> <br/> <p> <b>DATABASE: </b> {backupRecord.Name} <br/> <b>Resource Group: </b> {resourceGroup.Name} <br/> <b>Backup Date Utc: </b> {backupRecord.RegisteredDateUTC:yyyy-MM-dd HH:mm:ss} <br/> <b>Execution Status: </b> <span style='color:red;padding:3px'>{backupRecord.BackupStatus}</span> <br/> <b>Ref No #: </b> <span style='color:brown;padding:2px'>{backupRecord.Id}</span> </p> <p> Execution Message: <b><i>{backupRecord.ExecutionMessage}</i></b></p>       <br/><br/><br/><br> <span style='color:gray'>Powered By Crudsoft Technologies <br/>email: support@crudsofttechnologies.com</span>";
                    List<string> destinations = GetValidDestinations(resourceGroup.NotifyEmailDestinations);
                    await SendEmailAsync(subject, emailBody, destinations);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task SendContentDeliveryNotificationAsync(ContentDeliveryRecord record)
        {
            try
            {
                _logger.LogInformation("Received BackupRecordUpdated Notification....");
                if (record.CurrentStatus != BackupRecordBackupStatus.ERROR.ToString())
                    return;
                using (var scope = _scopeFactory.CreateScope())
                {
                    IResourceGroupRepository _resourceGroupPersistanceService = scope.ServiceProvider.GetRequiredService<IResourceGroupRepository>();
                    IBackupRecordRepository _backupRecordPersistanceService = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
                    //Info On Resource Group
                    ResourceGroup resourceGroup = await _resourceGroupPersistanceService.GetByIdOrKeyAsync(record.ResourceGroupId);
                    if (resourceGroup == null)
                        return; //No Valid Resource Group
                    if (!resourceGroup.NotifyOnErrorBackupDelivery)
                        return; //Disabled
                    BackupRecord backupRecord = await _backupRecordPersistanceService.GetByIdAsync(record.BackupRecordId);
                    if (backupRecord == null)
                        return; //No Valid Backup File
                    string subject = $"[{resourceGroup.Name}/{backupRecord.Name}] {record.DeliveryType} Failed";
                    string emailBody = $"<h3>[{resourceGroup.Name}/{backupRecord.Name}] {record.DeliveryType} Failed</h3> <br/> <p> <b>DELIVERY TYPE: </b> {record.DeliveryType} <br/>   <b>DATABASE: </b> {backupRecord.Name} <br/> <b>Resource Group: </b> {resourceGroup.Name} <br/> <b>Backup Date Utc: </b> {backupRecord.RegisteredDateUTC:yyyy-MM-dd HH:mm:ss} <br/>  <br/> <b>Execution Status: </b> <span style='color:red;padding:3px'>{record.CurrentStatus}</span> <br/><br/> <b>Ref No #: </b> <span style='color:brown;padding:2px'>{record.Id}</span> <br/> </p> <p> Execution Message: <b><i>{record.ExecutionMessage}</i></b></p>       <br/><br/><br/><br> <span style='color:gray'>Powered By Crudsoft Technologies <br/>email: support@crudsofttechnologies.com</span>";
                    List<string> destinations = GetValidDestinations(resourceGroup.NotifyEmailDestinations);
                    await SendEmailAsync(subject, emailBody, destinations);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task SendEmailAsync(string subject, string messageBody, List<string> destinations)
        {
            if (destinations == null || destinations.Count == 0)
                return;
            if (string.IsNullOrWhiteSpace(_options.SMTPEmailAddress) || string.IsNullOrWhiteSpace(_options.SMTPEmailCredentials) || string.IsNullOrWhiteSpace(_options.SMTPHost))
                return;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                using (MailMessage e_mail = new MailMessage())
                {
                    using (SmtpClient Smtp_Server = new SmtpClient())
                    {
                        //Configs
                        Smtp_Server.UseDefaultCredentials = false;
                        Smtp_Server.Credentials = new System.Net.NetworkCredential(_options.SMTPEmailAddress, _options.SMTPEmailCredentials);
                        Smtp_Server.Port = _options.SMTPPort; //Use 587
                        Smtp_Server.EnableSsl = _options.SMTPEnableSSL;
                        Smtp_Server.Host = _options.SMTPHost;
                        //Other Configs
                        e_mail.From = new MailAddress(_options.SMTPEmailAddress, _options.SMTPDefaultSMTPFromName);
                        //This Configs Should be Placed here
                        e_mail.Subject = subject;
                        e_mail.IsBodyHtml = true;
                        e_mail.Body = messageBody;

                        //Add Default
                        e_mail.To.Add(destinations[0]);

                        if (destinations.Count > 1)
                        {
                            int addIndex = 0;
                            foreach (string dest in destinations)
                            {
                                //Skip the First
                                if (addIndex != 0)
                                    e_mail.Bcc.Add(dest);
                                addIndex++;
                            }
                        }
                        //Finally Send
                        await Smtp_Server.SendMailAsync(e_mail);
                        _logger.LogInformation($"Sent Notifications to Resource Group addresses");
                    }
                }
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error Sending Notification to address", ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation($"Completed in {stopwatch.ElapsedMilliseconds:N0} Milliseconds");
            }
        }

        private List<string> GetValidDestinations(string destinations)
        {
            List<string> allEmails = new List<string>();
            if (destinations == null)
                return allEmails;
            string[] emailSplits = destinations?.Split(',');
            if (emailSplits.Length < 1)
                return allEmails;
            foreach (string email in emailSplits)
                if (!string.IsNullOrEmpty(email))
                    allEmails.Add(email.Replace(" ", string.Empty).Trim());
            return allEmails;
        }
    }
}
