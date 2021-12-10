using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SemanticBackup.WebClient.Services
{
    public class TimeZoneHelper
    {
        private readonly string _filePath;
        private readonly ILogger<TimeZoneHelper> _logger;
        private List<TimeZoneRecord> RecordCollection = new List<TimeZoneRecord>();
        public TimeZoneHelper(IWebHostEnvironment env, ILogger<TimeZoneHelper> logger)
        {
            this._filePath = string.Format("{0}\\{1}", env.ContentRootPath, "timezones.json");
            this._logger = logger;
        }
        public List<TimeZoneRecord> GetAll()
        {
            try
            {
                if (RecordCollection != null && RecordCollection.Count > 0)
                    return RecordCollection;
                if (!File.Exists(this._filePath))
                    return new List<TimeZoneRecord>();
                string fileContents = File.ReadAllText(this._filePath);
                RecordCollection = JsonConvert.DeserializeObject<List<TimeZoneRecord>>(fileContents);
                if (RecordCollection != null)
                    return RecordCollection;
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
            return new List<TimeZoneRecord>();
        }
    }
    public class TimeZoneRecord
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
