namespace ReactionLab.Identity.Persistence;

public static class DatabaseTime
{
    public static DateTimeOffset GetDatabaseUtcNow(this TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
