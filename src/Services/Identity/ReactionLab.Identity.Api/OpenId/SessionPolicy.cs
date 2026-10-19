namespace ReactionLab.Identity.Api.OpenId;

internal static class SessionPolicy
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromDays(14);
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan PushedRequestLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RedeemedTokenRetention = TimeSpan.FromDays(1);
}
