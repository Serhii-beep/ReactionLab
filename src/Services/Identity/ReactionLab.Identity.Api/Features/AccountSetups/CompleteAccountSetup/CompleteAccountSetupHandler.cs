using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Members;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.OpenId;
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
        if (!OneTimeLinkTokens.TryHash(command.Token, out var tokenHash)
            || await context.FindPendingAccountSetupAsync(tokenHash, timeProvider.GetUtcNow(), cancellationToken)
                is not { } setup
            || await userManager.FindByIdAsync(setup.AccountId.ToString()) is not { OrganizationId: { } } account)
        {
            return Result.Failure(AccountSetupErrors.LinkInvalid);
        }

        var username = await ChooseUsernameAsync(
            setup,
            account.OrganizationId.Value,
            command.Username,
            cancellationToken);

        if (username.IsFailure)
        {
            return Result.Failure(username.Error);
        }

        var passwordHash = await newPasswordCheck.GetValidHashAsync(account, command.NewPassword, cancellationToken);

        if (passwordHash.IsFailure)
        {
            return Result.Failure(passwordHash.Error);
        }

        CompletedAccountSetup? completed;

        try
        {
            completed = await CompleteAsync(account.Id, tokenHash, username.Value, passwordHash.Value);
        }
        catch (PostgresException ex) when (MemberConflicts.Of(ex) == MemberErrors.UsernameInUse)
        {
            return Result.Failure(AccountSetupErrors.UsernameInUse);
        }

        if (completed is null)
        {
            return Result.Failure(AccountSetupErrors.LinkInvalid);
        }

        logger.LogInformation("Account {AccountId} set its password through a setup link.", account.Id);

        return Result.Success();
    }

    private async Task<Result<string?>> ChooseUsernameAsync(
        PendingAccountSetup setup,
        Guid organizationId,
        string? chosenUsername,
        CancellationToken cancellationToken)
    {
        if (setup.Username is not null)
        {
            return Result.Success<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(chosenUsername))
        {
            return Result.Failure<string?>(AccountSetupErrors.UsernameRequired);
        }

        var username = User.NormalizeOrganizationUsername(chosenUsername);

        if (username.IsFailure)
        {
            return Result.Failure<string?>(username.Error);
        }

        var taken = await context.ListTakenUsernamesAsync(organizationId, [username.Value], cancellationToken);

        return taken.Count > 0
            ? Result.Failure<string?>(AccountSetupErrors.UsernameInUse)
            : Result.Success<string?>(username.Value);
    }

    private Task<CompletedAccountSetup?> CompleteAsync(
        Guid accountId,
        byte[] tokenHash,
        string? username,
        string passwordHash) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var securityStamp = RandomNumberGenerator.GetHexString(SecurityStampLength);
            var completed = await context.CompleteAccountSetupAsync(
                accountId,
                tokenHash,
                username,
                passwordHash,
                securityStamp,
                timeProvider.GetUtcNow());

            if (completed is null)
            {
                return await context.FindCompletedAccountSetupAsync(accountId, passwordHash);
            }

            await context.EndAccountSessionsAsync(completed.AccountId, securityStamp);

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
