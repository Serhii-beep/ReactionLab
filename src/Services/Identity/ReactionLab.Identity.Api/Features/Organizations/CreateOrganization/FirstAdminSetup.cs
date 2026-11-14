namespace ReactionLab.Identity.Api.Features.Organizations.CreateOrganization;

internal sealed record FirstAdminSetup(
    byte[]? TokenHash,
    bool Mailed,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
