namespace ReactionLab.Identity.Persistence.Accounts;

public sealed class SignInMethodChange
{
    public const int MaximumChangeLength = 32;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(2);

    private SignInMethodChange()
    {

    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Change { get; private set; } = string.Empty;

    public string? Provider { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public string? UserAgent { get; private set; }
}
