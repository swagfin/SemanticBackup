using CG.Web.MegaApiClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class UploaderMegaNxBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly ContentDeliveryRecord _contentDeliveryRecord;
        private readonly BackupRecord _backupRecord;
        private readonly ContentDeliveryConfiguration _contentDeliveryConfiguration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;
        public string BotId => _contentDeliveryRecord.Id;

        public UploaderMegaNxBot(BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IServiceScopeFactory scopeFactory, ILogger logger)
        {
            this._resourceGroupId = backupRecord.ResourceGroupId;
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._backupRecord = backupRecord;
            this._contentDeliveryConfiguration = contentDeliveryConfiguration;
            this._scopeFactory = scopeFactory;
            this._logger = logger;
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            MegaApiClient client = new MegaApiClient();
            try
            {
                _logger.LogInformation($"Uploading Backup File via MEGA nz....");
                await Task.Delay(new Random().Next(1000));
                RSMegaNxSetting settings = GetValidDeserializedSettings();
                stopwatch.Start();
                //Upload FTP
                CheckIfFileExistsOrRemove(this._backupRecord.Path);
                //FTP Upload
                string executionMessage = "MEGA nz Uploading...";
                //Directory
                string validDirectory = (string.IsNullOrWhiteSpace(settings.RemoteFolder)) ? "backups" : settings.RemoteFolder;
                validDirectory = validDirectory.Replace(" ", "_").Replace("/", "-");
                await client.LoginAsync(settings.Username, settings.Password);
                IEnumerable<INode> nodes = await client.GetNodesAsync();
                INode root = nodes.Single(x => x.Type == NodeType.Root);
                //Check if Folder Exists
                INode myFolder = client.GetNodes(root).FirstOrDefault(n => n.Type == NodeType.Directory && n.Name == validDirectory);
                if (myFolder == null)
                    myFolder = await client.CreateFolderAsync(validDirectory, root);
                //Upload File
                INode myFile = await client.UploadFileAsync(this._backupRecord.Path, myFolder);
                executionMessage = $"Uploaded to: {validDirectory}";
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, ContentDeliveryRecordStatus.READY.ToString(), executionMessage, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Uploading Backup File MEGA nz: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), (ex.InnerException != null) ? $"Error Uploading: {ex.InnerException.Message}" : ex.Message, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                try { if (client.IsLoggedIn) await client.LogoutAsync(); client = new MegaApiClient(); } catch { }
            }
        }

        private RSMegaNxSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSMegaNxSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSMegaNxSetting)}");
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
                using (var scope = _scopeFactory.CreateScope())
                {
                    IContentDeliveryRecordPersistanceService _persistanceService = scope.ServiceProvider.GetRequiredService<IContentDeliveryRecordPersistanceService>();
                    _persistanceService.UpdateStatusFeed(recordId, status, message, elapsed);
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
