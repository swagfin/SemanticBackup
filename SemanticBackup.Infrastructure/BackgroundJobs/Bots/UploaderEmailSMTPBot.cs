using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderEmailSMTPBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderEmailSMTPBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _contentDeliveryRecord.Id;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        public UploaderEmailSMTPBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<UploaderEmailSMTPBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Sending Email via SMTP....");
                await Task.Delay(new Random().Next(1000));
                SmtpDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Smtp ?? throw new Exception("no valid smtp config");
                stopwatch.Start();
                //Upload FTP
                CheckIfFileExistsOrRemove(this._backupRecord.Path);
                //Check any Recepient
                List<string> emailRecipients = settings.GetValidSmtpDestinations();
                if (emailRecipients.Count == 0)
                    throw new Exception("No recipients added in Resource Group SMTP");
                //proceed
                string fileName = Path.GetFileName(this._backupRecord.Path);
                string executionMessage = "Sending Email....";
                using (MailMessage e_mail = new MailMessage())
                {
                    using (SmtpClient Smtp_Server = new SmtpClient())
                    {
                        //Configs
                        Smtp_Server.UseDefaultCredentials = false;
                        Smtp_Server.Credentials = new System.Net.NetworkCredential(settings.SMTPEmailAddress, settings.SMTPEmailCredentials);
                        Smtp_Server.Port = settings.SMTPPort; //Use 587
                        Smtp_Server.EnableSsl = settings.SMTPEnableSSL;
                        Smtp_Server.Host = settings.SMTPHost;
                        //Attachment
                        byte[] fileContents;
                        using (StreamReader sourceStream = new StreamReader(this._backupRecord.Path))
                            fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync());
                        MemoryStream strm = new MemoryStream(fileContents);
                        Attachment AttachData = new Attachment(strm, fileName);
                        e_mail.Attachments.Add(AttachData);
                        //Other Configs
                        e_mail.From = new MailAddress(settings.SMTPEmailAddress, settings.SMTPDefaultSMTPFromName);
                        //This Configs Should be Placed here
                        e_mail.Subject = $"DATABASE BACKUP | {fileName}";
                        e_mail.IsBodyHtml = true;
                        e_mail.Body = $"Find Attached Database Backup Record: <br /> <b>File:</b> {fileName} <br/> <br/><br/><br> <span style='color:gray'>Powered By Crudsoft Technologies <br/>email: support@crudsofttechnologies.com</span>";

                        //Add Default
                        e_mail.To.Add(emailRecipients[0]);

                        if (emailRecipients.Count > 1)
                        {
                            int addIndex = 0;
                            foreach (string dest in emailRecipients)
                            {
                                //Skip the First
                                if (addIndex != 0)
                                    e_mail.Bcc.Add(dest);
                                addIndex++;
                            }
                        }

                        //Finally Send
                        await Smtp_Server.SendMailAsync(e_mail);
                        executionMessage = $"Sent to: {settings.SMTPDestinations}";
                    }
                }
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordDeliveryStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Sending Email via SMTP: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), (ex.InnerException != null) ? $"Error Uploading: {ex.InnerException.Message}" : ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private void CheckIfFileExistsOrRemove(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"No Database File In Path or May have been deleted, Path: {path}");
        }

        private void UpdateBackupFeed(string recordId, string status, string message, long elapsed)
        {
            try
            {

                using (var scope = _scopeFactory.CreateScope())
                {
                    IContentDeliveryRecordRepository _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordRepository>();
                    _persistanceService.UpdateStatusFeedAsync(recordId, status, message, elapsed);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error Updating Feed: " + ex.Message);
            }
            finally
            {
                IsCompleted = true;
            }
        }
    }
}
