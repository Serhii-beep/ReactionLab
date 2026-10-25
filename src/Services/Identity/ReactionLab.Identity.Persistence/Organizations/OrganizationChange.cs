namespace ReactionLab.Identity.Persistence.Organizations;

public sealed class OrganizationChange
{
    public const int MaximumChangeLength = 32;
    public const int MaximumRoleLength = 32;

    private OrganizationChange()
    {

    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Change { get; private set; } = string.Empty;

    public Guid? AccountId { get; private set; }

    public string? Role { get; private set; }

    public Guid? ChangedBy { get; private set; }

    public Guid? ChangedInSession { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
