namespace ReactionLab.Infrastructure.Caching;

public static class CacheExpiration
{
    public static readonly TimeSpan Long = TimeSpan.FromHours(24);

    public static readonly TimeSpan Medium = TimeSpan.FromHours(1);

    public static readonly TimeSpan Search = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan Usage = TimeSpan.FromDays(30);
}