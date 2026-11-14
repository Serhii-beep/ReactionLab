namespace ReactionLab.Identity.Api.Features.Accounts.ResetPassword;

internal sealed record PasswordResetOutcome(int EndedSessionCount, int RemovedLoginCount);
