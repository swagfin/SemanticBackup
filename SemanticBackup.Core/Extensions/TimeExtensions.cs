using System;

namespace SemanticBackup.Core
{
    public static class TimeExtensions
    {
        public static DateTime ConvertFromUTC(this DateTime dateTime, string timezone)
        {
            if (string.IsNullOrWhiteSpace(timezone))
            {
                timezone = "GMT Standard Time";
            }
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            dateTime = SetKind(dateTime);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, tz);
        }

        public static DateTime ConvertToUTC(this DateTime dateTime, string timezone)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            dateTime = SetKind(dateTime);
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, tz);
        }
        private static DateTime SetKind(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        public static DateTime IgnoreSeconds(this DateTime time, bool end)
        {
            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, 0, time.Kind);
            return end ? time.AddMinutes(1) : time;
        }
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Sunday)
        {
            int diff = (7 + (startOfWeek - dt.DayOfWeek)) % 7;
            return dt.AddDays(diff).Date;
        }
    }
}
