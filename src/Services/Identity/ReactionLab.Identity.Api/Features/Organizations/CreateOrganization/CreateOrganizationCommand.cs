namespace ReactionLab.Identity.Api.Features.Organizations.CreateOrganization;

internal sealed record CreateOrganizationCommand(
    Guid AdminId,
    Guid AdminSessionId,
    string? Kind,
    string? Name,
    Guid? DistrictId,
    string? FirstAdminDisplayName,
    string? FirstAdminUsername,
    string? FirstAdminEmail,
    string FirstAdminLocale);
