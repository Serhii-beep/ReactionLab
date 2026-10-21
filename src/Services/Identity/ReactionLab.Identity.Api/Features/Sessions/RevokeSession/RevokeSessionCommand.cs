using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeSession;

internal sealed record RevokeSessionCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    SessionSignIn SignIn,
    Guid SessionId);
