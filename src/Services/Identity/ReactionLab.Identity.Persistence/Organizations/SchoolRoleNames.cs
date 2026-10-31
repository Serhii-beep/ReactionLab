namespace ReactionLab.Identity.Persistence.Organizations;

public static class SchoolRoleNames
{
    public const string SchoolAdmin = "school-admin";
    public const string Teacher = "teacher";
    public const string Student = "student";
    public const string DistrictAdmin = "district-admin";

    public static IReadOnlyList<string> All { get; } = [SchoolAdmin, Teacher, Student, DistrictAdmin];

    public static string AdminOf(string organizationKind) =>
        organizationKind == OrganizationKinds.District ? DistrictAdmin : SchoolAdmin;

    public static bool FitsKind(string role, string organizationKind) =>
        (role == DistrictAdmin) == (organizationKind == OrganizationKinds.District);
}
