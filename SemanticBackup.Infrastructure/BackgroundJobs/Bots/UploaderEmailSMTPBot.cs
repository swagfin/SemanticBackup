using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderEmailSMTPBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(UploaderEmailSMTPBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public UploaderEmailSMTPBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord)
        {
            _contentDeliveryRecord = contentDeliveryRecord;
            _resourceGroup = resourceGroup;
            _backupRecord = backupRecord;
        }

        public async Task RunAsync(Func<BackupRecordDeliveryFeed, CancellationToken, Task> onDeliveryFeedUpdate, CancellationToken cancellationToken)
        {
            Status = BotStatus.Starting;
            Stopwatch stopwatch = new();
            try
            {
                Console.WriteLine($"uploading file to SMTP Mail: {_backupRecord.Path}");
                SmtpDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Smtp ?? throw new Exception("no valid smtp config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //check folder
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or May have been deleted, Path: {_backupRecord.Path}");
                //Check any Recepient
                List<string> emailRecipients = settings.GetValidSmtpDestinations();
                if (emailRecipients.Count == 0)
                    throw new Exception("No recipients added in Resource Group SMTP");
                //proceed
                string fileName = Path.GetFileName(this._backupRecord.Path);
                string executionMessage = "Sending Email....";
                using (MailMessage e_mail = new())
                {
                    using SmtpClient Smtp_Server = new();
                    //Configs
                    Smtp_Server.UseDefaultCredentials = false;
                    Smtp_Server.Credentials = new System.Net.NetworkCredential(settings.SMTPEmailAddress, settings.SMTPEmailCredentials);
                    Smtp_Server.Port = settings.SMTPPort;
                    Smtp_Server.EnableSsl = settings.SMTPEnableSSL;
                    Smtp_Server.Host = settings.SMTPHost;
                    //Attachment
                    byte[] fileContents;
                    using (StreamReader sourceStream = new(this._backupRecord.Path))
                        fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync(cancellationToken));
                    MemoryStream strm = new(fileContents);
                    Attachment AttachData = new(strm, fileName);
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
                    await Smtp_Server.SendMailAsync(e_mail, cancellationToken);
                    executionMessage = $"Sent to: {settings.SMTPDestinations}";
                }
                stopwatch.Stop();
                //notify update
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupDeliveryNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = _contentDeliveryRecord.Id,
                    Status = BackupRecordStatus.READY,
                    Message = executionMessage,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);

                Status = BotStatus.Completed;
            }
            catch (Exception ex)
            {
                Status = BotStatus.Error;
                Console.WriteLine(ex.Message);
                stopwatch.Stop();
                await onDeliveryFeedUpdate(new BackupRecordDeliveryFeed
                {
                    DeliveryFeedType = DeliveryFeedType.BackupDeliveryNotify,
                    BackupRecordId = _backupRecord.Id,
                    BackupRecordDeliveryId = _contentDeliveryRecord.Id,
                    Status = BackupRecordStatus.ERROR,
                    Message = (ex.InnerException != null) ? $"Error: {ex.InnerException.Message}" : ex.Message,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }, cancellationToken);
            }
        }
    }
}
