namespace ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;

internal sealed record RevokeOtherSessionsCommand(Guid AccountId, Guid CurrentSessionId, DateTimeOffset? SignedInAt);
