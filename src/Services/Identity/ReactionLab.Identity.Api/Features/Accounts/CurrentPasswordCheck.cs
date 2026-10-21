using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal sealed class CurrentPasswordCheck(
    AppDbContext context,
    UserManager<User> userManager,
    PasswordVerificationGate verificationGate,
    SessionRevocation sessionRevocation,
    TimeProvider timeProvider,
    ILogger<CurrentPasswordCheck> logger)
{
    public async Task<Result<string>> GetVerifiedHashAsync(
        User user,
        Guid sessionId,
        bool signedInWithPassword,
        string? currentPassword,
        CancellationToken cancellationToken)
    {
        if (user.PasswordHash is not { } passwordHash)
        {
            return Result.Failure<string>(AccountErrors.PasswordNotSet);
        }

        if (string.IsNullOrEmpty(currentPassword)
            || !UnicodeText.TryNormalize(currentPassword, out var normalizedPassword))
        {
            return Result.Failure<string>(AccountErrors.CurrentPasswordIncorrect);
        }

        if (!signedInWithPassword
            && !await context.ClaimCurrentPasswordCheckAsync(user.Id, PasswordPolicy.Lockout, timeProvider.GetUtcNow()))
        {
            logger.LogInformation(
                "Session {SessionId} may not check a password - account {AccountId} is locked after wrong ones.",
                sessionId,
                user.Id);

            return Result.Failure<string>(AccountErrors.PasswordCheckLocked);
        }

        var subject = user.Id.ToString();
        var checks = await context.ClaimPasswordCheckAsync(
            subject,
            sessionId,
            PasswordPolicy.CurrentPasswordChecksPerSession);

        if (checks == 0)
        {
            await sessionRevocation.RevokeAsync(sessionId.ToString(), CancellationToken.None);

            return Result.Failure<string>(SessionErrors.Ended);
        }

        var verification = await verificationGate.RunAsync(
            () => userManager.PasswordHasher.VerifyHashedPassword(user, passwordHash, normalizedPassword),
            cancellationToken);

        if (verification is PasswordVerificationResult.Failed)
        {
            return checks < PasswordPolicy.CurrentPasswordChecksPerSession
                ? Refuse(user, sessionId, checks)
                : await EndSessionAsync(user, sessionId);
        }

        await context.ResetSignInAttemptsAsync(user.Id);

        if (await context.MarkPasswordVerifiedAsync(subject, sessionId, timeProvider.GetUtcNow()) == 0)
        {
            return Result.Failure<string>(SessionErrors.Ended);
        }

        return passwordHash;
    }

    private Result<string> Refuse(User user, Guid sessionId, int checks)
    {
        logger.LogInformation(
            "Session {SessionId} gave a wrong current password for account {AccountId}.",
            sessionId,
            user.Id);

        return Result.Failure<string>(AccountErrors.CurrentPasswordIncorrect.WithArgs(
            ("remainingAttempts", PasswordPolicy.CurrentPasswordChecksPerSession - checks)));
    }

    private async Task<Result<string>> EndSessionAsync(User user, Guid sessionId)
    {
        await sessionRevocation.RevokeAsync(sessionId.ToString(), CancellationToken.None);

        logger.LogWarning(
            "Ended session {SessionId} of account {AccountId} after {AttemptCount} wrong current passwords.",
            sessionId,
            user.Id,
            PasswordPolicy.CurrentPasswordChecksPerSession);

        return Result.Failure<string>(SessionErrors.Ended);
    }
}
