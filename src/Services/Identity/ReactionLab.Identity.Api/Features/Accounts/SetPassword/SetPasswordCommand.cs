using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Accounts.SetPassword;

internal sealed record SetPasswordCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    SessionSignIn SignIn,
    string? NewPassword);
