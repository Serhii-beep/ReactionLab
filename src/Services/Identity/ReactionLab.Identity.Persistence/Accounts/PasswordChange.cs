namespace ReactionLab.Identity.Persistence.Accounts;

public sealed class PasswordChange
{
    public const int MaximumUserAgentLength = 256;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(2);

    private PasswordChange()
    {

    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public string? UserAgent { get; private set; }
}
