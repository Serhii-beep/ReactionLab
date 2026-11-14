using System.Globalization;
using ReactionLab.SharedKernel;

namespace ReactionLab.Notifications.Templates;

internal static class MailTimes
{
    private const string MomentFormat = "dddd, d MMMM, HH:mm";
    private const string DateFormat = "dddd, d MMMM";

    public static string DescribeMoment(DateTimeOffset moment, string? timeZone, string locale)
    {
        var local = ToZone(moment, timeZone);

        return $"{local.ToString(MomentFormat, CultureInfo.GetCultureInfo(locale))} {GmtOffset(local.Offset)}";
    }

    public static string DescribeDate(DateTimeOffset moment, string? timeZone, string locale) =>
        ToZone(moment, timeZone).ToString(DateFormat, CultureInfo.GetCultureInfo(locale));

    private static DateTimeOffset ToZone(DateTimeOffset moment, string? timeZone) =>
        TimeZoneInfo.ConvertTime(moment, IanaTimeZones.Find(timeZone) ?? TimeZoneInfo.Utc);

    private static string GmtOffset(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero)
        {
            return "GMT";
        }

        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var hours = Math.Abs(offset.Hours);
        var minutes = Math.Abs(offset.Minutes);

        return minutes == 0
            ? string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{hours}")
            : string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{hours}:{minutes:00}");
    }
}
