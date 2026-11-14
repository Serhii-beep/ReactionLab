namespace ReactionLab.SharedKernel;

public static class IanaTimeZones
{
    private const string ProbeZone = "America/New_York";

    public static TimeZoneInfo? Find(string? timeZone) =>
        !string.IsNullOrWhiteSpace(timeZone)
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.Trim(), out var zone)
        && zone.HasIanaId
            ? zone
            : null;

    public static void RequireSupport()
    {
        if (Find(ProbeZone) is null)
        {
            throw new InvalidOperationException(
                "Time zones need the IANA tz database. Install tzdata, or run with ICU globalization.");
        }
    }
}
