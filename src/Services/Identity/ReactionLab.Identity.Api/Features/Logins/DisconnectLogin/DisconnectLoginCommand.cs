using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Logins.DisconnectLogin;

internal sealed record DisconnectLoginCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    SessionSignIn SignIn,
    string Provider);
