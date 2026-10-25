namespace ReactionLab.Identity.Persistence.Organizations;

public sealed class UserSchoolRole
{
    public const int MaximumRoleLength = 32;

    private UserSchoolRole()
    {

    }

    public Guid UserId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Role { get; private set; } = string.Empty;
}
