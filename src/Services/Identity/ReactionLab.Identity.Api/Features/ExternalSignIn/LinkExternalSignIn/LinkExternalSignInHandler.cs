using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
using ReactionLab.Identity.Api.Features.Logins;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.LinkExternalSignIn;

internal sealed class LinkExternalSignInHandler(
    PendingExternalSignIns pendingSignIns,
    ExternalSignInProviders providers,
    PasswordSignInVerifier passwordVerifier,
    AppDbContext context,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    SignInMethodChanges signInMethodChanges,
    ILogger<LinkExternalSignInHandler> logger) : ICommandHandler<LinkExternalSignInCommand>
{
    public async ValueTask<Result> HandleAsync(LinkExternalSignInCommand command, CancellationToken cancellationToken)
    {
        if (await pendingSignIns.FindAsync() is not { } pending
            || providers.Find(pending.Identity.Provider) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.NotPending);
        }

        var identity = pending.Identity;
        var user = await passwordVerifier.VerifyWithProvenEmailAsync(
            identity.VerifiedEmail,
            command.Password,
            cancellationToken);

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        var added = await AddLoginAsync(
            user.Value.Id,
            identity.Provider,
            identity.Subject,
            provider.DisplayName,
            command.UserAgent);

        if (added == 0)
        {
            var linked = await userManager.FindByLoginAsync(identity.Provider, identity.Subject);

            if (linked is null)
            {
                return Result.Failure(LoginErrors.AlreadyConnected);
            }

            if (linked.Id != user.Value.Id)
            {
                return Result.Failure(ExternalSignInErrors.AlreadyLinked);
            }
        }

        await pendingSignIns.EndAsync();
        await signInManager.SignInWithClaimsAsync(
            user.Value,
            isPersistent: false,
            [
                new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Password),
                new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Federated)
            ]);

        if (added == 1)
        {
            logger.LogInformation("Linked {Provider} to account {AccountId}.", identity.Provider, user.Value.Id);
        }

        return Result.Success();
    }

    private Task<int> AddLoginAsync(
        Guid accountId,
        string providerName,
        string subject,
        string providerDisplayName,
        string? userAgent)
    {
        var changeId = Guid.CreateVersion7();

        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var added = await context.AddLoginAsync(accountId, providerName, subject, providerDisplayName);

            if (added == 1)
            {
                await signInMethodChanges.RecordAsync(
                    accountId,
                    changeId,
                    SignInMethodChangeNames.ProviderConnected,
                    providerName,
                    userAgent);
            }

            await transaction.CommitAsync(CancellationToken.None);

            return added;
        });
    }
}
