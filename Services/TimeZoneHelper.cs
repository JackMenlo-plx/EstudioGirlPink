namespace StudioGirlPink.Services;

public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> _mexicoTimeZone = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    });

    public static TimeZoneInfo MexicoTimeZone => _mexicoTimeZone.Value;

    public static DateTime ToMexicoTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.ToUniversalTime(), MexicoTimeZone);
    }

    public static DateTime MexicoToUtc(this DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        var unspec = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspec, MexicoTimeZone);
    }
}
