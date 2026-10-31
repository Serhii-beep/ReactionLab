namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record AccountOrganizationResponse(
    Guid OrganizationId,
    string Kind,
    string Name,
    IReadOnlyList<string> Roles,
    bool Suspended,
    bool Active);
