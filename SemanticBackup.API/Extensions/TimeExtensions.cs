using System;

namespace SemanticBackup.API.Extensions
{
    public static class TimeExtensions
    {
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
