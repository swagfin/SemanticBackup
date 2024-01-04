using Dropbox.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticBackup.Infrastructure.BackgroundJobs.Bots
{
    internal class InDepthDeleteDropboxBot : IBot
    {
        private readonly BackupRecordDelivery _contentDeliveryRecord;
        private readonly ResourceGroup _resourceGroup;
        private readonly BackupRecord _backupRecord;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InDepthDeleteDropboxBot> _logger;
        public bool IsCompleted { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;

        public string ResourceGroupId => _resourceGroup.Id;
        public string BotId => _contentDeliveryRecord.Id;
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        public InDepthDeleteDropboxBot(ResourceGroup resourceGroup, BackupRecord backupRecord, BackupRecordDelivery contentDeliveryRecord, IServiceScopeFactory scopeFactory)
        {
            this._contentDeliveryRecord = contentDeliveryRecord;
            this._resourceGroup = resourceGroup;
            this._backupRecord = backupRecord;
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
                DropboxDeliveryConfig settings = _resourceGroup.BackupDeliveryConfig.Dropbox ?? throw new Exception("no valid dropbox config");
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
    }
}
