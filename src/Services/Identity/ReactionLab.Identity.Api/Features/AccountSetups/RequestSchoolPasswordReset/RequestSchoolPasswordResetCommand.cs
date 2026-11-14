namespace ReactionLab.Identity.Api.Features.AccountSetups.RequestSchoolPasswordReset;

internal sealed record RequestSchoolPasswordResetCommand(string? SignInCode, string? Username);
