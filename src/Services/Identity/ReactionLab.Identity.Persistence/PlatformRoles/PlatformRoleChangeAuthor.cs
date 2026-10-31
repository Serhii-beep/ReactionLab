namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed record PlatformRoleChangeAuthor(string Source, Guid? AccountId, Guid? SessionId)
{
    public const string AdminSource = "admin";
    public const string FirstAdminSource = "first-admin";
    public const string BreakGlassSource = "break-glass";
    public const string SchoolAccountSource = "school-account";

    public static readonly PlatformRoleChangeAuthor FirstAdmin = new(FirstAdminSource, null, null);
    public static readonly PlatformRoleChangeAuthor BreakGlass = new(BreakGlassSource, null, null);

    public static PlatformRoleChangeAuthor Admin(Guid accountId, Guid sessionId) =>
        new(AdminSource, accountId, sessionId);
}
