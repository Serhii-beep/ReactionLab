using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SetPassword;

internal sealed class SetPasswordHandler(
    UserManager<User> userManager,
    RecentSignInCheck recentSignInCheck,
    NewPasswordCheck newPasswordCheck,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<SetPasswordHandler> logger) : ICommandHandler<SetPasswordCommand>
{
    public async ValueTask<Result> HandleAsync(SetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (await userManager.FindByIdAsync(command.AccountId.ToString()) is not { } user)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        if (user.OrganizationId is not null)
        {
            return Result.Failure(AccountErrors.ManagedBySchool);
        }

        if (user.PasswordHash is not null)
        {
            return Result.Failure(AccountErrors.PasswordAlreadySet);
        }

        if (user.NormalizedEmail is null)
        {
            return Result.Failure(AccountErrors.PasswordNeedsEmail);
        }

        if (user.LockoutEnd == DateTimeOffset.MaxValue)
        {
            return Result.Failure(AccountErrors.PasswordSignInDisabled);
        }

        var proof = await recentSignInCheck.CheckSignInMethodChangeAsync(
            command.AccountId,
            command.CurrentSessionId,
            command.SignIn,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not set a password on account {AccountId}: {ErrorCode}.",
                command.CurrentSessionId,
                command.AccountId,
                proof.Error.Code);

            return proof;
        }

        var passwordHash = await newPasswordCheck.GetValidHashAsync(user, command.NewPassword, cancellationToken);

        if (passwordHash.IsFailure)
        {
            return Result.Failure(passwordHash.Error);
        }

        var probationEndsAt = timeProvider.GetUtcNow() + PasswordPolicy.Probation;
        var saved = await SaveAsync(command, passwordHash.Value, probationEndsAt);

        if (saved.IsSuccess)
        {
            logger.LogInformation(
                "Session {SessionId} set a password on account {AccountId}, on probation until {ProbationEndsAt}.",
                command.CurrentSessionId,
                command.AccountId,
                probationEndsAt);
        }

        return saved;
    }

    private Task<Result> SaveAsync(SetPasswordCommand command, string passwordHash, DateTimeOffset probationEndsAt) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            if (await context.SetPasswordAsync(command.AccountId, passwordHash, probationEndsAt) == 0)
            {
                return Result.Failure(AccountErrors.PasswordAlreadySet);
            }

            if (!await context.LockValidSessionAsync(command.AccountId.ToString(), command.CurrentSessionId))
            {
                return Result.Failure(SessionErrors.Ended);
            }

            await transaction.CommitAsync(CancellationToken.None);

            return Result.Success();
        });
}
