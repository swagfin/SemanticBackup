using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SemanticBackup.Core.Extensions;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class UploaderLinkGenBot : IBot
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
        public string BotId => _contentDeliveryRecord.Id;
        public UploaderLinkGenBot(BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IContentDeliveryRecordPersistanceService persistanceService, ILogger logger)
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
                _logger.LogInformation($"Creating Download Link....");
                await Task.Delay(new Random().Next(1000));
                RSDownloadLinkSetting settings = GetValidDeserializedSettings();
                stopwatch.Start();
                string contentLink = 5.GenerateUniqueId();
                if (settings.DownloadLinkType == "LONG")
                    contentLink = string.Format("{0}?token={1}", 55.GenerateUniqueId(), $"{this._backupRecord.Id}|{this._contentDeliveryConfiguration.Id}".ToMD5String());
                //Job to Do
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, ContentDeliveryRecordStatus.READY.ToString(), contentLink, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation($"Creating Download Link: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                stopwatch.Stop();
                UpdateBackupFeed(_contentDeliveryRecord.Id, BackupRecordBackupStatus.ERROR.ToString(), ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private RSDownloadLinkSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSDownloadLinkSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSDownloadLinkSetting)}");
            return config;
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
