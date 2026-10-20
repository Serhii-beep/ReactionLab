using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.ChangePassword;

internal sealed class ChangePasswordHandler(
    AppDbContext context,
    UserManager<User> userManager,
    PasswordVerificationGate verificationGate,
    SecurityStampRotation securityStampRotation,
    SessionRevocation sessionRevocation,
    ILogger<ChangePasswordHandler> logger) : ICommandHandler<ChangePasswordCommand>
{
    public async ValueTask<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (await userManager.FindByIdAsync(command.AccountId.ToString()) is not { } user)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        if (user.PasswordHash is not { } passwordHash
            || string.IsNullOrEmpty(command.CurrentPassword)
            || !UnicodeText.TryNormalize(command.CurrentPassword, out var currentPassword))
        {
            return Result.Failure(AccountErrors.CurrentPasswordIncorrect);
        }

        var checks = await context.ClaimPasswordCheckAsync(
            command.AccountId.ToString(),
            command.CurrentSessionId,
            PasswordPolicy.CurrentPasswordChecksPerSession);

        if (checks == 0)
        {
            await sessionRevocation.RevokeAsync(command.CurrentSessionId.ToString(), CancellationToken.None);

            return Result.Failure(SessionErrors.Ended);
        }

        var verification = await verificationGate.RunAsync(
            () => userManager.PasswordHasher.VerifyHashedPassword(user, passwordHash, currentPassword),
            cancellationToken);

        if (verification is PasswordVerificationResult.Failed)
        {
            return checks < PasswordPolicy.CurrentPasswordChecksPerSession
                ? RefuseCurrentPassword(command, checks)
                : await EndSessionAsync(command);
        }

        await context.ResetPasswordChecksAsync(command.CurrentSessionId);
        await context.ResetSignInAttemptsAsync(user.Id);

        var newPassword = await ValidateNewPasswordAsync(user, command.NewPassword);

        if (newPassword.IsFailure)
        {
            return Result.Failure(newPassword.Error);
        }

        var replacementHash = await verificationGate.RunAsync(
            () => userManager.PasswordHasher.HashPassword(user, newPassword.Value),
            cancellationToken);

        var rotation = await securityStampRotation.ReplacePasswordAsync(
            command.AccountId,
            command.CurrentSessionId,
            passwordHash,
            replacementHash);

        if (!rotation.AccountMatched)
        {
            logger.LogInformation(
                "Session {SessionId} lost its password change on account {AccountId} to another change.",
                command.CurrentSessionId,
                command.AccountId);

            return Result.Failure(AccountErrors.CurrentPasswordIncorrect);
        }

        if (!rotation.ActingSessionValid)
        {
            logger.LogInformation(
                "Session {SessionId} ended during its password change on account {AccountId}. Nothing changed.",
                command.CurrentSessionId,
                command.AccountId);

            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {SessionId} changed the password of account {AccountId}, ending {SessionCount} other session(s).",
            command.CurrentSessionId,
            command.AccountId,
            rotation.EndedSessionCount);

        return Result.Success();
    }

    private Result RefuseCurrentPassword(ChangePasswordCommand command, int checks)
    {
        logger.LogInformation(
            "Session {SessionId} gave a wrong current password for account {AccountId}.",
            command.CurrentSessionId,
            command.AccountId);

        return Result.Failure(AccountErrors.CurrentPasswordIncorrect.WithArgs(
            ("remainingAttempts", PasswordPolicy.CurrentPasswordChecksPerSession - checks)));
    }

    private async Task<Result> EndSessionAsync(ChangePasswordCommand command)
    {
        await sessionRevocation.RevokeAsync(command.CurrentSessionId.ToString(), CancellationToken.None);

        logger.LogWarning(
            "Ended session {SessionId} of account {AccountId} after {AttemptCount} wrong current passwords.",
            command.CurrentSessionId,
            command.AccountId,
            PasswordPolicy.CurrentPasswordChecksPerSession);

        return Result.Failure(SessionErrors.Ended);
    }

    private async Task<Result<string>> ValidateNewPasswordAsync(User user, string? newPassword)
    {
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, newPassword);

            if (!validation.Succeeded)
            {
                return validation.Errors.First().ToError() with { Field = nameof(ChangePasswordRequest.NewPassword) };
            }
        }

        return newPassword ?? string.Empty;
    }
}
