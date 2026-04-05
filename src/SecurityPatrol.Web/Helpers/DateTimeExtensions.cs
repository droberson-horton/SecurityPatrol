namespace SecurityPatrol.Web.Helpers;

public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a UTC DateTime to the specified IANA timezone and formats it.
    /// Falls back to server local time if timeZoneId is null or unrecognised.
    /// </summary>
    public static string ToTzString(this DateTime utcDateTime, string? timeZoneId, string format)
    {
        if (string.IsNullOrEmpty(timeZoneId))
            return utcDateTime.ToLocalTime().ToString(format);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), tz);
            return local.ToString(format);
        }
        catch
        {
            return utcDateTime.ToLocalTime().ToString(format);
        }
    }

    /// <summary>Same as ToTzString but accepts a nullable DateTime.</summary>
    public static string ToTzString(this DateTime? utcDateTime, string? timeZoneId, string format, string fallback = "")
    {
        return utcDateTime.HasValue ? utcDateTime.Value.ToTzString(timeZoneId, format) : fallback;
    }
}
