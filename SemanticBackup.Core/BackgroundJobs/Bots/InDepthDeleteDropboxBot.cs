using Dropbox.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Core.BackgroundJobs.Bots
{
    internal class InDepthDeleteDropboxBot : IBot
    {
        private readonly string _resourceGroupId;
        private readonly ContentDeliveryRecord _contentDeliveryRecord;
        private readonly BackupRecord _backupRecord;
        private readonly ContentDeliveryConfiguration _contentDeliveryConfiguration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InDepthDeleteDropboxBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroupId;
        public string BotId => _contentDeliveryRecord.Id;
        public InDepthDeleteDropboxBot(string resourceGroupId, BackupRecord backupRecord, ContentDeliveryRecord contentDeliveryRecord, ContentDeliveryConfiguration contentDeliveryConfiguration, IServiceScopeFactory scopeFactory)
        {
            this._resourceGroupId = resourceGroupId;
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._backupRecord = backupRecord;
            this._contentDeliveryConfiguration = contentDeliveryConfiguration;
            this._scopeFactory = scopeFactory;
            //Logger
            using (var scope = _scopeFactory.CreateScope())
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<InDepthDeleteDropboxBot>>();
        }
        public async Task RunAsync()
        {
            this.IsStarted = true;
            this.IsCompleted = false;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _logger.LogInformation($"DELETING Backup File From DropBox....");
                await Task.Delay(new Random().Next(1000));
                RSDropBoxSetting settings = GetValidDeserializedSettings();
                stopwatch.Start();
                //Directory
                string validDirectory = (string.IsNullOrWhiteSpace(settings.Directory)) ? "/" : settings.Directory;
                validDirectory = (validDirectory.EndsWith("/")) ? validDirectory : validDirectory + "/";
                validDirectory = (validDirectory.StartsWith("/")) ? validDirectory : "/" + validDirectory;
                //Filename
                string fileName = Path.GetFileName(this._backupRecord.Path);
                //Proceed
                if (string.IsNullOrWhiteSpace(settings.AccessToken))
                    throw new Exception("Access Token is NULL");
                //Proceed
                using (DropboxClient dbx = new DropboxClient(settings.AccessToken.Trim()))
                {
                    string initialFileName = string.Format("{0}{1}", validDirectory, fileName);
                    Dropbox.Api.Files.DeleteResult delResponse = await dbx.Files.DeleteV2Async(initialFileName, null);
                }
                stopwatch.Stop();
                _logger.LogInformation($"DELETING Backup File From DropBox: {_backupRecord.Path}... SUCCESS");
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex.Message);
                stopwatch.Stop();
            }
            finally
            {
                this.IsCompleted = true;
            }
        }

        private RSDropBoxSetting GetValidDeserializedSettings()
        {
            var config = JsonConvert.DeserializeObject<RSDropBoxSetting>(this._contentDeliveryConfiguration.Configuration);
            if (config == null)
                throw new Exception($"Invalid Configuration String provided Of Type: {nameof(RSDropBoxSetting)}");
            return config;
        }

    }
}
