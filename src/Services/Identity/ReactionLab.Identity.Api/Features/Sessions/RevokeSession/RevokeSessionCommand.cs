namespace ReactionLab.Identity.Api.Features.Sessions.RevokeSession;

internal sealed record RevokeSessionCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    DateTimeOffset? SignedInAt,
    Guid SessionId);
