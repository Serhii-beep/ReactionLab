namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed class AccountSetup
{
    public const int MaximumPurposeLength = 16;
    public const int MaximumMailedSetupsPerWindow = 5;
    public const int MaximumSelfResetsPerWindow = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan SelfResetLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan MailedSetupWindow = TimeSpan.FromDays(1);
    public static readonly TimeSpan SelfResetWindow = TimeSpan.FromDays(1);
    public static readonly TimeSpan MinimumIntervalBetweenSelfResets = TimeSpan.FromMinutes(5);

    private AccountSetup()
    {

    }

    public Guid UserId { get; private set; }

    public byte[]? TokenHash { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid? IssuedBy { get; private set; }

    public Guid? IssuedInSession { get; private set; }

    public Guid? RequestId { get; private set; }

    public DateTimeOffset? RequestedAt { get; private set; }

    public string? Email { get; private set; }

    public string? Purpose { get; private set; }

    public DateTimeOffset? FirstRequestedAt { get; private set; }

    public int RequestCount { get; private set; }
}
