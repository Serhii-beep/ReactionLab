namespace ReactionLab.Identity.Api.Features.Accounts.ChangePassword;

internal sealed record ChangePasswordCommand(
    Guid AccountId,
    Guid CurrentSessionId,
    string? CurrentPassword,
    string? NewPassword);
