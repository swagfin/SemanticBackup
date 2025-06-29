using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SemanticBackup.Core
{
    public static class DateTimeExtensions
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

        public static DateTime AdjustWithTimezoneOffset(this DateTime dateTime, string timezoneOffset = "00:00")
        {
            try
            {
                DateTime utcDate = TimeZoneInfo.ConvertTimeToUtc(dateTime);
                // Parse and calculate timezone offset
                int sign = 1;
                int hours = 0;
                int minutes = 0;

                Match offsetMatch = Regex.Match(timezoneOffset ?? "00:00", @"([+-])(\d{2}|00):(\d{2}|00)");
                if (offsetMatch.Success)
                {
                    sign = offsetMatch.Groups[1].Value == "+" ? 1 : -1;
                    hours = int.Parse(offsetMatch.Groups[2].Value);
                    minutes = int.Parse(offsetMatch.Groups[3].Value);
                }
                // Calculate the offset in milliseconds
                int offsetMilliseconds = (hours * 60 + minutes) * 60 * 1000 * sign;
                // Adjust the date with the offset
                DateTime adjustedDate = utcDate.AddMilliseconds(offsetMilliseconds);
                return adjustedDate;
            }
            catch { return dateTime; }
        }
        public static string ToLastRunPreviewableWithTimezone(this DateTime lastRunDateUtc, string timezoneOffset = "00:00")
        {
            if ((DateTime.UtcNow.Date - lastRunDateUtc.Date).TotalDays > 1000)
                return "Never";
            else
                return string.Format("{0:yyyy-MM-dd HH:mm}", lastRunDateUtc.AdjustWithTimezoneOffset(timezoneOffset));
        }
        public static (string timezone, string offset) ToTimezoneWithOffset(this TimeZoneInfo timeZoneInfo)
        {
            TimeSpan utcOffset = timeZoneInfo.BaseUtcOffset;
            return (timeZoneInfo.Id, (utcOffset >= TimeSpan.Zero ? "+" : "-") + utcOffset.ToString("hh':'mm"));
        }
        public static string ToTimezoneWithOffsetString(this TimeZoneInfo timeZoneInfo)
        {
            (string timezone, string offset) timezoneWithOffset = ToTimezoneWithOffset(timeZoneInfo);
            return string.Format("{0} ({1})", timezoneWithOffset.timezone, timezoneWithOffset.offset);
        }

        public static string ToUtcDifferenceString(this DateTime targetUtc, DateTime? compareUtc = null)
        {
            DateTime baseTime = compareUtc ?? DateTime.UtcNow;
            TimeSpan diff = targetUtc - baseTime;
            if (diff.TotalSeconds <= 0) return "Expired";

            int days = diff.Days;
            int hours = diff.Hours;
            int minutes = diff.Minutes;

            List<string> parts = [];
            if (days > 0) parts.Add($"{days} day(s)");
            if (hours > 0) parts.Add($"{hours}hr{(hours == 1 ? "" : "s")}");
            if (minutes > 0) parts.Add($"{minutes}min");

            return parts.Count > 0 ? string.Join(" ", parts) : "Expired";
        }
    }
}
