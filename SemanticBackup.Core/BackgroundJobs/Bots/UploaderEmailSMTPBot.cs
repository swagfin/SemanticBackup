using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class UploaderEmailSMTPBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly ContentDeliveryRecord _contentDeliveryRecord;
        private readonly BackupRecord _backupRecord;
        private readonly ContentDeliveryConfiguration _contentDeliveryConfiguration;
        private readonly IContentDeliveryRecordPersistanceService _persistanceService;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;

        public UploaderEmailSMTPBot(BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IContentDeliveryRecordPersistanceService persistanceService, ILogger logger)
        {
            this._resourceGroupId = backupRecord.ResourceGroupId;
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._backupRecord = backupRecord;
            this._contentDeliveryConfiguration = contentDeliveryConfiguration;
            this._persistanceService = persistanceService;
            this._logger = logger;
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
                RSEmailSMTPSetting settings = GetValidDeserializedSettings();
                stopwatch.Start();
                //Upload FTP
                CheckIfFileExistsOrRemove(this._backupRecord.Path);
                //Check any Recepient
                if (settings.ValidSMTPDestinations == null || settings.ValidSMTPDestinations.Count < 1)
                    throw new Exception("No recipients added in Resource Group SMTP");
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
                            fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                        MemoryStream strm = new MemoryStream(fileContents);
                        Attachment AttachData = new Attachment(strm, fileName);
                        e_mail.Attachments.Add(AttachData);
                        //Other Configs
                        e_mail.From = new MailAddress(settings.SMTPEmailAddress, settings.SMTPDefaultSMTPFromName);
                        //This Configs Should be Placed here
                        e_mail.Subject = $"DATABASE BACKUP | {fileName}";
                        e_mail.IsBodyHtml = true;
                        e_mail.Body = $"Find Attached Database Backup Record: <br /> <b>File:</b> {fileName} <br/> <br/><br/><br> <span style='color:gray'>Powered By Crudsoft Technologies <br/>email: support@cursofttechnologies.com</span>";

                        //Add Default
                        e_mail.To.Add(settings.ValidSMTPDestinations[0]);

                        if (settings.ValidSMTPDestinations.Count > 1)
                        {
                            int addIndex = 0;
                            foreach (string dest in settings.ValidSMTPDestinations)
                            {
                                //Skip the First
                                if (addIndex != 0)
                                    e_mail.Bcc.Add(dest);
                                addIndex++;
                            }
                        }

                        //Finally Send
                        await Task.Run(() => Smtp_Server.Send(e_mail));
                        executionMessage = $"Sent To: {settings.SMTPDestinations}";
                    }
                }
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, ContentDeliveryRecordStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Sending Email via SMTP: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), (ex.InnerException != null) ? $"Error Uploading: {ex.InnerException.Message}" : ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private RSEmailSMTPSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSEmailSMTPSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSEmailSMTPSetting)}");
            return config;
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
                _persistanceService.UpdateStatusFeed(recordId, status, message, elapsed);
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
