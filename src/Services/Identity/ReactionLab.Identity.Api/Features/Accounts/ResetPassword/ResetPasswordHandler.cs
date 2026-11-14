using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.ResetPassword;

internal sealed class ResetPasswordHandler(
    UserManager<User> userManager,
    NewPasswordCheck newPasswordCheck,
    PasswordChanges passwordChanges,
    AppDbContext context,
    ILogger<ResetPasswordHandler> logger) : ICommandHandler<ResetPasswordCommand>
{
    private const int SecurityStampLength = 32;

    public async ValueTask<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!OneTimeLinkTokens.TryHash(command.Token, out var tokenHash)
            || await context.FindPasswordResetAccountAsync(tokenHash, cancellationToken) is not { } accountId
            || await userManager.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            return Result.Failure(AccountErrors.PasswordResetLinkInvalid);
        }

        var passwordHash = await newPasswordCheck.GetValidHashAsync(account, command.NewPassword, cancellationToken);

        if (passwordHash.IsFailure)
        {
            return Result.Failure(passwordHash.Error);
        }

        var changeId = Guid.CreateVersion7();

        if (await ResetAsync(account.Id, tokenHash, passwordHash.Value, changeId, command.UserAgent)
            is not { } reset)
        {
            return Result.Failure(AccountErrors.PasswordResetLinkInvalid);
        }

        logger.LogInformation(
            "Account {AccountId} reset its password through a mailed link, ending {SessionCount} session(s) and "
            + "{LoginCount} login(s) connected before its address was confirmed.",
            account.Id,
            reset.EndedSessionCount,
            reset.RemovedLoginCount);

        return Result.Success();
    }

    private Task<PasswordResetOutcome?> ResetAsync(
        Guid accountId,
        byte[] tokenHash,
        string passwordHash,
        Guid changeId,
        string? userAgent) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var securityStamp = RandomNumberGenerator.GetHexString(SecurityStampLength);
            var completed = await context.ResetPasswordWithLinkAsync(
                accountId,
                tokenHash,
                passwordHash,
                securityStamp,
                PasswordPolicy.Probation);

            if (completed is null)
            {
                return await context.HoldsPasswordHashAsync(accountId, passwordHash)
                    ? new PasswordResetOutcome(0, 0)
                    : null;
            }

            var removedLoginCount = completed.AddressWasConfirmed ? 0 : await context.DeleteLoginsAsync(accountId);
            var endedSessionCount = await context.EndAccountSessionsAsync(accountId, securityStamp);

            await passwordChanges.RecordAsync(accountId, changeId, userAgent);
            await transaction.CommitAsync(CancellationToken.None);

            return new PasswordResetOutcome(endedSessionCount, removedLoginCount);
        });
}
