using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;

internal sealed record RevokeOtherSessionsCommand(Guid AccountId, Guid CurrentSessionId, SessionSignIn SignIn);
