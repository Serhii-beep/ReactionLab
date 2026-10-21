using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Accounts.Reauthenticate;

internal sealed record ReauthenticateCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    SessionSignIn SignIn,
    string? CurrentPassword);
