using Microsoft.Extensions.Options;
using SemanticBackup.Core;
using System;

namespace SemanticBackup.API.Services.Implementations
{
    public class ServerSharedRuntime : IServerSharedRuntime
    {
        private PersistanceOptions Options { get; }
        public TimeZoneInfo CurrentTimeZone { get; }
        public ServerSharedRuntime(IOptions<PersistanceOptions> persistanceOptions)
        {
            this.Options = persistanceOptions.Value;
            string timezone = string.IsNullOrWhiteSpace(Options.ServerDefaultTimeZone) ? "E. Africa Standard Time" : Options.ServerDefaultTimeZone;
            this.CurrentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }

        public DateTime GetServerTime
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
