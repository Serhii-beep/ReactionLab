namespace ReactionLab.Identity.Persistence.PlatformRoles;

public static class PlatformRoleNames
{
    public const string Admin = "admin";
    public const string Moderator = "moderator";
    public const string CatalogEditor = "catalog-editor";

    public static readonly IReadOnlyList<string> All = [Admin, Moderator, CatalogEditor];

    public static bool IsKnown(string role) => All.Contains(role, StringComparer.Ordinal);
}
