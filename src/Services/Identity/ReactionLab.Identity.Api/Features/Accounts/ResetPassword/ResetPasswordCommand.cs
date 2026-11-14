namespace ReactionLab.Identity.Api.Features.Accounts.ResetPassword;

internal sealed record ResetPasswordCommand(string? Token, string? NewPassword, string? UserAgent);
