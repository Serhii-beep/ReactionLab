using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.PlatformRoles;

namespace ReactionLab.Identity.Migrator;

internal sealed class PlatformAdminSeeding(
    AppDbContext context,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<PlatformAdminSeeding> logger)
{
    public const string FirstPlatformAdminEmailKey = "Identity:FirstPlatformAdminEmail";
    public const string BreakGlassPlatformAdminAccountIdKey = "Identity:BreakGlassPlatformAdminAccountId";

    private static readonly UpperInvariantLookupNormalizer Normalizer = new();

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (configuration[FirstPlatformAdminEmailKey] is { Length: > 0 } firstPlatformAdminEmail)
        {
            await SeedFirstPlatformAdminAsync(firstPlatformAdminEmail, cancellationToken);
        }

        if (configuration[BreakGlassPlatformAdminAccountIdKey] is { Length: > 0 } breakGlassAccountId)
        {
            await RestorePlatformAdminAsync(breakGlassAccountId, cancellationToken);
        }
    }

    private Task SeedFirstPlatformAdminAsync(string configuredAddress, CancellationToken cancellationToken)
    {
        var address = User.NormalizeEmail(configuredAddress);

        if (address.IsFailure)
        {
            throw new InvalidOperationException($"'{FirstPlatformAdminEmailKey}' is not a valid email address.");
        }

        var normalizedEmail = Normalizer.NormalizeEmail(address.Value);

        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            await context.LockPlatformRoleChangesAsync();

            if (await context.HasPlatformRoleChangesAsync(cancellationToken))
            {
                logger.LogInformation(
                    "Platform roles were granted before, so {Setting} is not used.",
                    FirstPlatformAdminEmailKey);
                return;
            }

            var candidate = await context.FindPlatformRoleCandidateByEmailAsync(normalizedEmail, cancellationToken);

            if (candidate is null)
            {
                logger.LogWarning(
                    "No account has the address in {Setting}. Register it, then run the migrator again.",
                    FirstPlatformAdminEmailKey);
                return;
            }

            if (!candidate.EmailConfirmed && !environment.IsDevelopment())
            {
                logger.LogWarning(
                    "The address in {Setting} isn't confirmed, so it isn't made a platform admin. "
                    + "Sign up with Google, then set a password: an admin acts with it once its probation ends.",
                    FirstPlatformAdminEmailKey);
                return;
            }

            var granted = await context.GrantPlatformRoleAsync(
                candidate.AccountId,
                PlatformRoleNames.Admin,
                PlatformRoleChangeAuthor.FirstAdmin);

            if (granted == 0)
            {
                logger.LogWarning(
                    "Account {AccountId} could not be made the first platform admin.",
                    candidate.AccountId);
                return;
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Made account {AccountId} the first platform admin, from {Setting}. "
                + "Address confirmed: {EmailConfirmed}.",
                candidate.AccountId,
                FirstPlatformAdminEmailKey,
                candidate.EmailConfirmed);
        });
    }

    private Task RestorePlatformAdminAsync(string configuredAccountId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(configuredAccountId, out var accountId))
        {
            throw new InvalidOperationException($"'{BreakGlassPlatformAdminAccountIdKey}' is not an account id.");
        }

        logger.LogWarning(
            "{Setting} is set. Remove it once the account is a platform admin again.",
            BreakGlassPlatformAdminAccountIdKey);

        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            await context.LockPlatformRoleChangesAsync();

            if (await context.FindPlatformRoleCandidateAsync(accountId, cancellationToken) is null)
            {
                logger.LogWarning("No account has the id in {Setting}.", BreakGlassPlatformAdminAccountIdKey);
                return;
            }

            if (await context.HoldsPlatformRoleAsync(accountId, PlatformRoleNames.Admin, cancellationToken))
            {
                logger.LogInformation("Account {AccountId} is already a platform admin.", accountId);
                return;
            }

            if (await context.HasBreakGlassGrantAsync(accountId, cancellationToken))
            {
                logger.LogWarning(
                    "Account {AccountId} was made a platform admin through {Setting} before and has lost it since, "
                    + "so the setting is not used again.",
                    accountId,
                    BreakGlassPlatformAdminAccountIdKey);
                return;
            }

            var granted = await context.GrantPlatformRoleAsync(
                accountId,
                PlatformRoleNames.Admin,
                PlatformRoleChangeAuthor.BreakGlass);

            if (granted == 0)
            {
                logger.LogWarning("Account {AccountId} could not be made a platform admin.", accountId);
                return;
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogWarning(
                "Made account {AccountId} a platform admin through {Setting}.",
                accountId,
                BreakGlassPlatformAdminAccountIdKey);
        });
    }
}
