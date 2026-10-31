namespace ReactionLab.Identity.Persistence.Organizations;

public static class MemberStatuses
{
    public const string Active = "active";
    public const string Pending = "pending";
    public const string Locked = "locked";
    public const string Disabled = "disabled";

    public static IReadOnlyList<string> All { get; } = [Active, Pending, Locked, Disabled];

    public static string Of(SchoolMember member, DateTimeOffset now) => member switch
    {
        { DisabledAt: not null } => Disabled,
        { HasPassword: false } => Pending,
        _ when member.LockoutEnd > now => Locked,
        _ => Active
    };
}
