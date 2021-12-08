using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SemanticBackup.WebClient.Services.Implementations
{
    public class DirectoryStorageService : IDirectoryStorageService
    {
        private readonly ILogger<DirectoryStorageService> _logger;

        public string DirectorySavingFile { get; }
        public List<ActiveDirectory> CurrentDirectories { get { return Directories.ActiveDirectories; } private set { Directories.ActiveDirectories = value; } }

        public DirectoryStorageService(IWebHostEnvironment webHostEnvironment, IConfiguration configuration, ILogger<DirectoryStorageService> logger)
        {
            string activeDirectoriesFileName = configuration.GetValue<string>("ActiveDirectoriesFileName") ?? "active-directories.temp.json";
            this.DirectorySavingFile = string.Format("{0}\\data\\{1}", webHostEnvironment.ContentRootPath, activeDirectoriesFileName);
            this._logger = logger;
        }

        public void InitDirectories()
        {
            _logger.LogInformation("Initializing Active Directories");
            if (!File.Exists(this.DirectorySavingFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.DirectorySavingFile));
                File.WriteAllText(this.DirectorySavingFile, "[]", Encoding.UTF8);
                this.CurrentDirectories = new List<ActiveDirectory>();
                return;
            }
            //Retrive
            string directoryContents = File.ReadAllText(this.DirectorySavingFile, Encoding.UTF8);
            if (string.IsNullOrEmpty(directoryContents))
            {
                File.WriteAllText(this.DirectorySavingFile, "[]");
                this.CurrentDirectories = new List<ActiveDirectory>();
                return;
            }
            var directories = JsonConvert.DeserializeObject<List<ActiveDirectory>>(directoryContents);
            this.CurrentDirectories = directories ?? new List<ActiveDirectory>();
        }
        private void SaveCurrentDirectory()
        {
            try
            {
                if (this.CurrentDirectories != null && this.CurrentDirectories.Count > 0)
                {
                    var nullDirectories = this.CurrentDirectories.Where(x => string.IsNullOrWhiteSpace(x.Id)).ToList();
                    if (nullDirectories != null && nullDirectories.Count > 0)
                        foreach (var dir in nullDirectories)
                            this.CurrentDirectories.Remove(dir);
                }
                string serializedContents = JsonConvert.SerializeObject(this.CurrentDirectories, Formatting.Indented);
                File.WriteAllText(this.DirectorySavingFile, serializedContents, Encoding.UTF8);
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to persist Directory to File, Error: {ex.Message}"); }
        }

        public List<ActiveDirectory> GetActiveDirectories()
        {
            return this.CurrentDirectories.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();
        }
        public bool AddDirectory(ActiveDirectory apiDirectory)
        {
            var existingDirectory = this.CurrentDirectories.Where(x => !string.IsNullOrWhiteSpace(x.Id)).FirstOrDefault(x => x.Id == apiDirectory.Id);
            if (existingDirectory != null)
                throw new Exception($"Directory with Id: {existingDirectory.Id} already exists, Directory not Saved");
            this.CurrentDirectories.Add(apiDirectory);
            SaveCurrentDirectory();
            return true;
        }

        public bool RemoveDirectory(string id)
        {
            var existingDirectory = this.CurrentDirectories.FirstOrDefault(x => x.Id == id);
            if (existingDirectory == null)
                return false;
            this.CurrentDirectories.Remove(existingDirectory);
            SaveCurrentDirectory();
            return true;
        }

        public bool UpdateDirectory(ActiveDirectory apiDirectory)
        {
            var existingDirectory = this.CurrentDirectories.FirstOrDefault(x => x.Id == apiDirectory.Id);
            if (existingDirectory == null)
                return false;
            existingDirectory.Url = apiDirectory.Url;
            existingDirectory.Name = apiDirectory.Name;
            SaveCurrentDirectory();
            return true;
        }

        public bool SwitchToDirectory(string id)
        {
            var existingDirectory = this.CurrentDirectories.FirstOrDefault(x => x.Id == id);
            if (existingDirectory == null)
                return false;
            if (long.TryParse(DateTime.Now.ToString("yyyyMMddHHmmss"), out long lastAccess))
            {
                existingDirectory.LastAccess = lastAccess;
                SaveCurrentDirectory();
                return true;
            }
            return true;
        }

        public bool SwitchToDirectory(ActiveDirectory directory)
        {
            if (directory == null)
                return false;
            return SwitchToDirectory(directory.Id);
        }

        public ActiveDirectory GetActiveDirectory(string id)
        {
            return this.CurrentDirectories.FirstOrDefault(x => x.Id == id);
        }
    }
}
