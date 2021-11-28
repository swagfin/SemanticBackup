using SemanticBackup.Core;
using System;

namespace SemanticBackup.API.Core
{
    public class SharedTimeZone
    {
        private PersistanceOptions Options { get; }
        public TimeZoneInfo CurrentTimeZone { get; }
        public SharedTimeZone(PersistanceOptions persistanceOptions)
        {
            this.Options = persistanceOptions;
            string timezone = string.IsNullOrWhiteSpace(Options.ServerDefaultTimeZone) ? "E. Africa Standard Time" : Options.ServerDefaultTimeZone;
            this.CurrentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }

        public DateTime Now
        {
            get
            {
                try
                {
                    DateTime dateTime = DateTime.UtcNow;

                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                    DateTime finalDate = TimeZoneInfo.ConvertTimeFromUtc(dateTime, this.CurrentTimeZone);
                    return finalDate;
                }
                catch { }
                return DateTime.Now;
            }
        }

    }
}
