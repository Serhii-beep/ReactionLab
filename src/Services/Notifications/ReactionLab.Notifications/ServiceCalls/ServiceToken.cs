namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class ServiceToken(string value, DateTimeOffset expiresAt)
{
    public string Value { get; } = value;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;
}
