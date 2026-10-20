using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
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
        var user = await passwordVerifier.VerifyAsync(identity.VerifiedEmail, command.Password, cancellationToken);

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        if (!await TryAddLoginAsync(user.Value, identity, provider)
            && (await userManager.FindByLoginAsync(identity.Provider, identity.Subject))?.Id != user.Value.Id)
        {
            return Result.Failure(ExternalSignInErrors.AlreadyLinked);
        }

        await pendingSignIns.EndAsync();
        await signInManager.SignInWithClaimsAsync(
            user.Value,
            isPersistent: false,
            [
                new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Password),
                new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Federated)
            ]);

        logger.LogInformation("Linked {Provider} to account {AccountId}.", identity.Provider, user.Value.Id);

        return Result.Success();
    }

    private async Task<bool> TryAddLoginAsync(User user, ExternalIdentity identity, IExternalSignInProvider provider)
    {
        context.UserLogins.Add(new IdentityUserLogin<Guid>
        {
            LoginProvider = identity.Provider,
            ProviderKey = identity.Subject,
            ProviderDisplayName = provider.DisplayName,
            UserId = user.Id
        });

        try
        {
            await context.SaveChangesAsync(CancellationToken.None);

            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.ChangeTracker.Clear();

            return false;
        }
    }
}
