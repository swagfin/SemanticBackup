using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SemanticBackup.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemanticBackup.Services
{
    public class TimeZoneHelper
    {
        private readonly string _filePath;
        private readonly ILogger<TimeZoneHelper> _logger;
        private readonly SystemConfigOptions _options;
        private List<string> RecordCollection = new List<string>();
        public TimeZoneHelper(ILogger<TimeZoneHelper> logger, SystemConfigOptions options)
        {
            this._filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "timezones.json");
            this._logger = logger;
            this._options = options;
        }
        public List<string> GetAll()
        {
            try
            {
                if (RecordCollection != null && RecordCollection.Count > 0)
                    return RecordCollection;
                if (!File.Exists(this._filePath))
                    return new List<string>();
                string fileContents = File.ReadAllText(this._filePath);
                // Determine to use Utc TimeZone or ks
                if (_options.IsLinuxEnv)
                    RecordCollection = JsonConvert.DeserializeObject<List<TimeZoneRecordWithUtc>>(fileContents)?.Select(x => x.utc).SelectMany(x => x).Distinct().ToList();
                else
                    RecordCollection = JsonConvert.DeserializeObject<List<TimeZoneRecord>>(fileContents)?.Select(x => x.Value).Distinct().ToList();
                return RecordCollection ?? new List<string>();
            }
            catch (Exception ex) { _logger.LogWarning(ex.Message); }
            return new List<string>();
        }
    }
    public class TimeZoneRecord
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
    public class TimeZoneRecordWithUtc : TimeZoneRecord
    {
        public string[] utc { get; set; }
    }
}
