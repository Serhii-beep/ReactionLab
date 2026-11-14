using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Templates;

internal static class SchoolRoles
{
    private const string DistrictAdmin = "district-admin";
    private const string SchoolAdmin = "school-admin";
    private const string Teacher = "teacher";
    private const string Student = "student";

    public static string Describe(ITranslationCatalog catalog, string locale, IReadOnlyList<string> roles) =>
        catalog.Translate(
            locale,
            roles switch
            {
                _ when roles.Contains(DistrictAdmin) => "schoolRoles.districtAdmin",
                _ when roles.Contains(SchoolAdmin) => "schoolRoles.schoolAdmin",
                _ when roles.Contains(Teacher) => "schoolRoles.teacher",
                _ when roles.Contains(Student) => "schoolRoles.student",
                _ => "schoolRoles.member"
            });
}
