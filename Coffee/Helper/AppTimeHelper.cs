namespace Coffee.Helper
{
    public static class AppTimeHelper
    {
        // ===========================
        // 🕐 UTC
        // ===========================
        public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        // ===========================
        // 🌐 DYNAMIC
        // ===========================
        public static DateTimeOffset NowAt(string timezoneId)
        {
            var tz = ResolveTimeZone(timezoneId);
            return TimeZoneInfo.ConvertTime(UtcNow, tz);
        }

        public static DateTimeOffset ConvertTo(DateTimeOffset value, string timezoneId)
        {
            var tz = ResolveTimeZone(timezoneId);
            // If the value came from DB as datetime2, it lost its UTC offset and might have the local machine's offset.
            // We force it to be treated as UTC before converting.
            var dtUtc = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc);
            var utcOffset = new DateTimeOffset(dtUtc);
            return TimeZoneInfo.ConvertTime(utcOffset, tz);
        }

        // ===========================
        // 🔧 INTERNAL
        // ===========================
        private static TimeZoneInfo ResolveTimeZone(params string[] timezoneIds)
        {
            foreach (var id in timezoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Utc;
        }
    }
}