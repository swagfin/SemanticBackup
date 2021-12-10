using SemanticBackup.Core;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;

namespace SemanticBackup.API.Core
{
    public class SharedTimeZone
    {
        private readonly IResourceGroupPersistanceService resourceGroupPersistanceService;
        public string DefaultTimezone { get; }

        public SharedTimeZone(PersistanceOptions persistanceOptions, IResourceGroupPersistanceService resourceGroupPersistanceService)
        {
            this.resourceGroupPersistanceService = resourceGroupPersistanceService;
            this.DefaultTimezone = string.IsNullOrWhiteSpace(persistanceOptions.ServerDefaultTimeZone) ? "GMT Standard Time" : persistanceOptions.ServerDefaultTimeZone;
        }

        public DateTime ConvertLocalTimeToUtc(DateTime dateTimeLocal, string timezone)
        {

            try
            {
                string tzIdentifier = string.IsNullOrWhiteSpace(timezone) ? timezone : DefaultTimezone;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzIdentifier);
                dateTimeLocal = DateTime.SpecifyKind(dateTimeLocal, DateTimeKind.Unspecified);
                DateTime finalDateUTC = TimeZoneInfo.ConvertTimeToUtc(dateTimeLocal, tz);
                return finalDateUTC;
            }
            catch { }
            return DateTime.UtcNow;
        }
        public DateTime ConvertUtcDateToLocalTime(DateTime dateTimeUTC, string timezone)
        {

            try
            {
                string tzIdentifier = string.IsNullOrWhiteSpace(timezone) ? timezone : DefaultTimezone;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzIdentifier);
                dateTimeUTC = DateTime.SpecifyKind(dateTimeUTC, DateTimeKind.Unspecified);
                DateTime finalLocalTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUTC, tz);
                return finalLocalTime;
            }
            catch { }
            return DateTime.UtcNow;
        }

        public DateTime GetLocalTimeByResourceGroupId(string resourceGroupId)
        {

            try
            {
                ResourceGroup resourceGroup = resourceGroupPersistanceService.GetById(resourceGroupId);
                if (resourceGroup == null)
                    return DateTime.UtcNow;
                return GetLocalTimeByTimezone(resourceGroup.TimeZone);
            }
            catch { }
            return DateTime.UtcNow;
        }

        public DateTime GetLocalTimeByTimezone(string timezone)
        {

            try
            {
                string tzIdentifier = string.IsNullOrWhiteSpace(timezone) ? timezone : DefaultTimezone;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzIdentifier);
                DateTime dateTime = DateTime.UtcNow;
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                DateTime finalDate = TimeZoneInfo.ConvertTimeFromUtc(dateTime, tz);
                return finalDate;
            }
            catch { }
            return DateTime.UtcNow;
        }

    }
}
