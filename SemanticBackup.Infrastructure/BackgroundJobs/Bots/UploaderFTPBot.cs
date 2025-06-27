using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class UploaderFTPBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public string BotId => $"{_resourceGroup.Id}::{_backupRecord.Id}::{nameof(UploaderFTPBot)}";
        public string ResourceGroupId => _resourceGroup.Id;
        public BotStatus Status { get; internal set; } = BotStatus.NotReady;

        public UploaderFTPBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord)
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
                Console.WriteLine($"uploading file to FTP Server: {_backupRecord.Path}");
                FtpDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Ftp ?? throw new Exception("no valid ftp config");
                stopwatch.Start();
                Status = BotStatus.Running;
                //check file
                if (!File.Exists(_backupRecord.Path))
                    throw new Exception($"No Database File In Path or May have been deleted, Path: {_backupRecord.Path}");
                //FTP Upload
                string executionMessage = "FTP Uploading...";
                //Directory
                string validDirectory = (string.IsNullOrWhiteSpace(settings.Directory)) ? "/" : settings.Directory;
                validDirectory = validDirectory.EndsWith('/') ? validDirectory : validDirectory + "/";
                validDirectory = validDirectory.StartsWith('/') ? validDirectory : "/" + validDirectory;
                string validServerName = settings.Server.Replace("ftp", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty);
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                try
                {
                    string fullServerUrl = $"ftp://{validServerName}{validDirectory}{fileName}";
                    byte[] fileContents;
                    using (FileStream sourceStream = File.OpenRead(this._backupRecord.Path))
                    {
                        fileContents = new byte[sourceStream.Length];
                        await sourceStream.ReadAsync(fileContents, cancellationToken);
                    }

#pragma warning disable SYSLIB0014 // Type or member is obsolete
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullServerUrl);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                    request.EnableSsl = false; // Set to true if your FTP server uses FTPS
                    request.UsePassive = true;
                    request.UseBinary = true;
                    request.KeepAlive = false;
                    request.ContentLength = fileContents.Length;

                    // Write to the request stream
                    using (Stream requestStream = await request.GetRequestStreamAsync())
                    {
                        await requestStream.WriteAsync(fileContents, 0, fileContents.Length, cancellationToken);
                    }

                    // Get the response to ensure upload completed
                    using FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                    if (response.StatusCode == FtpStatusCode.ClosingData)
                    {
                        executionMessage = $"Uploaded to Server: {settings.Server}";
                    }
                    else
                    {
                        throw new Exception($"Failed to upload. FTP status: {response.StatusDescription}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Upload failed: {ex.Message}");
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

                Console.WriteLine($"Successfully uploaded file to FTP Server: {_backupRecord.Path}");
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
