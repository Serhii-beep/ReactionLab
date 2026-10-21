using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Accounts.ChangePassword;

internal sealed record ChangePasswordCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    SessionSignIn SignIn,
    string? CurrentPassword,
    string? NewPassword);
