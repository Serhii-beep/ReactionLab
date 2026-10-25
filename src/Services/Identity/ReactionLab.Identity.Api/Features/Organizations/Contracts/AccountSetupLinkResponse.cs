namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record AccountSetupLinkResponse(
    Guid AccountId,
    string Username,
    string SetupToken,
    DateTimeOffset SetupExpiresAt);
