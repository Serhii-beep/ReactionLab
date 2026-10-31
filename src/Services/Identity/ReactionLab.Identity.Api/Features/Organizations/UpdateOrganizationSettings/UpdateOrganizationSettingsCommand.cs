namespace ReactionLab.Identity.Api.Features.Organizations.UpdateOrganizationSettings;

internal sealed record UpdateOrganizationSettingsCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    string? Name,
    string? Locale,
    string? TimeZone,
    string? SignInCode,
    bool? TeachersMayResetStudentPasswords);
