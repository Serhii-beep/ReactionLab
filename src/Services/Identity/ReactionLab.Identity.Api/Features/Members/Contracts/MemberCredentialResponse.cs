namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record MemberCredentialResponse(
    string Kind,
    string? Password,
    string? SetupToken,
    DateTimeOffset? SetupExpiresAt);
