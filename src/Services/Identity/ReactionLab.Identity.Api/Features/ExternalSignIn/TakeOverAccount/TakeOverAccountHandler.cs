using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.TakeOverAccount;

internal sealed class TakeOverAccountHandler(
    PendingExternalSignIns pendingSignIns,
    ExternalSignInProviders providers,
    AppDbContext context,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<TakeOverAccountHandler> logger) : ICommandHandler<TakeOverAccountCommand>
{
    private const int SecurityStampLength = 32;

    public async ValueTask<Result> HandleAsync(TakeOverAccountCommand command, CancellationToken cancellationToken)
    {
        if (await pendingSignIns.FindAsync() is not { } pending
            || providers.Find(pending.Identity.Provider) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.NotPending);
        }

        var identity = pending.Identity;
        var address = User.NormalizeEmail(identity.VerifiedEmail);

        if (identity.VerifiedEmail is null
            || address.IsFailure
            || await userManager.FindByEmailAsync(address.Value) is not
            {
                EmailConfirmed: false,
                OrganizationId: null,
                PasswordHash: not null,
                NormalizedEmail: { } normalizedEmail
            } account)
        {
            return Result.Failure(ExternalSignInErrors.TakeoverUnavailable);
        }

        var takeover = await TakeOverAsync(account.Id, normalizedEmail, identity, provider.DisplayName);

        if (takeover.IsFailure)
        {
            return Result.Failure(takeover.Error);
        }

        await context.Entry(account).ReloadAsync(CancellationToken.None);
        await pendingSignIns.EndAsync();
        await signInManager.SignInWithClaimsAsync(
            account,
            new AuthenticationProperties { IssuedUtc = pending.SignedInAt },
            [new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Federated)]);

        logger.LogInformation(
            "Took over never-confirmed account {AccountId} through {Provider}, ending {SessionCount} session(s).",
            account.Id,
            identity.Provider,
            takeover.Value);

        return Result.Success();
    }

    private Task<Result<int>> TakeOverAsync(
        Guid accountId,
        string normalizedEmail,
        ExternalIdentity identity,
        string providerDisplayName) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var securityStamp = RandomNumberGenerator.GetHexString(SecurityStampLength);

            if (!await context.TakeOverUnconfirmedAccountAsync(accountId, normalizedEmail, securityStamp))
            {
                return await context.HoldsLoginAsync(accountId, identity.Provider, identity.Subject)
                    ? Result.Success(0)
                    : Result.Failure<int>(ExternalSignInErrors.TakeoverUnavailable);
            }

            await context.DeleteSecurityLinksAsync(accountId);
            await context.DeleteLoginsAsync(accountId);

            var endedSessionCount = await context.EndAccountSessionsAsync(accountId, securityStamp);

            if (await context.AddLoginAsync(accountId, identity.Provider, identity.Subject, providerDisplayName) == 0)
            {
                return Result.Failure<int>(ExternalSignInErrors.AlreadyLinked);
            }

            await transaction.CommitAsync(CancellationToken.None);

            return Result.Success(endedSessionCount);
        });
}
