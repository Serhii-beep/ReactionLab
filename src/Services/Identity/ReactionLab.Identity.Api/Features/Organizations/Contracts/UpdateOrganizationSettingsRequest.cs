namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record UpdateOrganizationSettingsRequest(
    string? Name,
    string? Locale,
    string? TimeZone,
    string? SignInCode,
    bool? TeachersMayResetStudentPasswords);
