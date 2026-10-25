namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed class AccountSetup
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private AccountSetup()
    {

    }

    public Guid UserId { get; private set; }

    public byte[] TokenHash { get; private set; } = [];

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid? IssuedBy { get; private set; }

    public Guid? IssuedInSession { get; private set; }
}
