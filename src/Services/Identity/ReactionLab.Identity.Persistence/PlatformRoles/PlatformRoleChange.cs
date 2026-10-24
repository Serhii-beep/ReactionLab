namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed class PlatformRoleChange
{
    public const int MaximumRoleLength = 32;
    public const int MaximumSourceLength = 16;

    private PlatformRoleChange()
    {

    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public bool Granted { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public Guid? ChangedBy { get; private set; }

    public Guid? ChangedInSession { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
