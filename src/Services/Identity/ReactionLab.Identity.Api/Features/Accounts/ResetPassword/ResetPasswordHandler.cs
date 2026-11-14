using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.ResetPassword;

internal sealed class ResetPasswordHandler(
    UserManager<User> userManager,
    NewPasswordCheck newPasswordCheck,
    PasswordChanges passwordChanges,
    AppDbContext context,
    OutboxWriter outbox,
    TimeProvider timeProvider,
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

        var ownerProfile = account.EmailConfirmed ? null : CheckOwnerProfile(command);

        if (ownerProfile is { IsFailure: true })
        {
            if (ownerProfile.Error == User.SchoolAccountRequired)
            {
                await context.DeleteSecurityLinksAsync(account.Id);

                logger.LogInformation("Refused a reset of account {AccountId} that needs a school account.", account.Id);
            }

            return Result.Failure(ownerProfile.Error);
        }

        var passwordHash = await newPasswordCheck.GetValidHashAsync(account, command.NewPassword, cancellationToken);

        if (passwordHash.IsFailure)
        {
            return Result.Failure(passwordHash.Error);
        }

        var changeId = Guid.CreateVersion7();

        if (await ResetAsync(account.Id, tokenHash, passwordHash.Value, ownerProfile?.Value, changeId, command.UserAgent)
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

    private Result<RegisteredProfile> CheckOwnerProfile(ResetPasswordCommand command)
    {
        var locale = SupportedLocale.Create(command.Locale);

        return locale.IsFailure
            ? locale.Error
            : User.CheckRegisteredProfile(
                command.DisplayName,
                locale.Value,
                command.BirthYear,
                command.TimeZone,
                timeProvider.GetUtcNow());
    }

    private Task<PasswordResetOutcome?> ResetAsync(
        Guid accountId,
        byte[] tokenHash,
        string passwordHash,
        RegisteredProfile? ownerProfile,
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

            if (!completed.AddressWasConfirmed && ownerProfile is not null)
            {
                var version = await context.ReplaceRegisteredProfileAsync(accountId, ownerProfile);

                await outbox.InsertAsync(
                    context,
                    new UserProfile(accountId, version, ownerProfile.DisplayName, ownerProfile.PreferredLocale));
            }

            var endedSessionCount = await context.EndAccountSessionsAsync(accountId, securityStamp);

            await passwordChanges.RecordAsync(accountId, changeId, userAgent);
            await transaction.CommitAsync(CancellationToken.None);

            return new PasswordResetOutcome(endedSessionCount, removedLoginCount);
        });
}
