using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.AccountSetups.CompleteAccountSetup;

internal sealed class CompleteAccountSetupHandler(
    UserManager<User> userManager,
    NewPasswordCheck newPasswordCheck,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<CompleteAccountSetupHandler> logger) : ICommandHandler<CompleteAccountSetupCommand>
{
    private const int SecurityStampLength = 32;

    public async ValueTask<Result> HandleAsync(CompleteAccountSetupCommand command, CancellationToken cancellationToken)
    {
        if (!AccountSetupTokens.TryHash(command.Token, out var tokenHash)
            || await context.FindPendingAccountSetupAsync(tokenHash, timeProvider.GetUtcNow(), cancellationToken)
                is not { } setup
            || await userManager.FindByIdAsync(setup.AccountId.ToString()) is not { } account)
        {
            return Result.Failure(AccountSetupErrors.LinkInvalid);
        }

        var passwordHash = await newPasswordCheck.GetValidHashAsync(account, command.NewPassword, cancellationToken);

        if (passwordHash.IsFailure)
        {
            return Result.Failure(passwordHash.Error);
        }

        if (await CompleteAsync(account.Id, tokenHash, passwordHash.Value) is null)
        {
            return Result.Failure(AccountSetupErrors.LinkInvalid);
        }

        logger.LogInformation("Account {AccountId} set its password through a setup link.", account.Id);

        return Result.Success();
    }

    private Task<CompletedAccountSetup?> CompleteAsync(Guid accountId, byte[] tokenHash, string passwordHash) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var completed = await context.CompleteAccountSetupAsync(
                accountId,
                tokenHash,
                passwordHash,
                RandomNumberGenerator.GetHexString(SecurityStampLength),
                timeProvider.GetUtcNow());

            if (completed is null)
            {
                return await context.FindCompletedAccountSetupAsync(accountId, passwordHash);
            }

            await context.RecordOrganizationChangeAsync(
                completed.OrganizationId,
                OrganizationChangeNames.SetupCompleted,
                completed.AccountId,
                role: null,
                completed.AccountId,
                changedInSession: null);

            await transaction.CommitAsync(CancellationToken.None);

            return completed;
        });
}
