using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record OrganizationSettingsResponse(
    Guid Id,
    string Kind,
    string Name,
    string Locale,
    string TimeZone,
    string SignInCode,
    bool TeachersMayResetStudentPasswords)
{
    public static OrganizationSettingsResponse From(Organization organization) =>
        new(
            organization.Id,
            organization.Kind,
            organization.Name,
            organization.Locale,
            organization.TimeZone,
            organization.SignInCode,
            organization.TeachersMayResetStudentPasswords);
}
