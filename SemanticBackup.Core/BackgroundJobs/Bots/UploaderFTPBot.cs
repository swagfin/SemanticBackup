﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class UploaderFTPBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UploaderFTPBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _contentDeliveryRecord.Id;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        public UploaderFTPBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<UploaderFTPBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"Uploading Backup File via FTP....");
                await Task.Delay(new Random().Next(1000));
                FtpDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Ftp ?? throw new Exception("no valid ftp config");
                stopwatch.Start();
                //Upload FTP
                CheckIfFileExistsOrRemove(this._backupRecord.Path);
                //FTP Upload
                string executionMessage = "FTP Uploading...";
                //Directory
                string validDirectory = (string.IsNullOrWhiteSpace(settings.Directory)) ? "/" : settings.Directory;
                validDirectory = (validDirectory.EndsWith("/")) ? validDirectory : validDirectory + "/";
                validDirectory = (validDirectory.StartsWith("/")) ? validDirectory : "/" + validDirectory;
                string validServerName = settings.Server.Replace("ftp", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty);
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                try
                {
                    // Get the object used to communicate with the server.
                    string fullServerUrl = string.Format("ftp://{0}{1}{2}", validServerName, validDirectory, fileName);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullServerUrl);
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                    byte[] fileContents;
                    using (StreamReader sourceStream = new StreamReader(this._backupRecord.Path))
                    {
                        fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync());
                    }
                    request.ContentLength = fileContents.Length;
                    using (Stream requestStream = await request.GetRequestStreamAsync())
                    {
                        await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
                    }
                    using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                    {
                        executionMessage = $"Uploaded to Server: {settings.Server}";
                    }
                }
                catch (WebException e)
                {
                    throw new Exception(e.Message);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordDeliveryStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Uploading Backup File via FTP: {_backupRecord.Path}... SUCCESS");
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
