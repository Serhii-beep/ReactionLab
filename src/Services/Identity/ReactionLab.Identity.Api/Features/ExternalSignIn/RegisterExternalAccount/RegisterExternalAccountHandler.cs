using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.RegisterExternalAccount;

internal sealed class RegisterExternalAccountHandler(
    PendingExternalSignIns pendingSignIns,
    ExternalSignInProviders providers,
    ExternalSignInAddresses addresses,
    AppDbContext context,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    TimeProvider timeProvider,
    ILogger<RegisterExternalAccountHandler> logger) : ICommandHandler<RegisterExternalAccountCommand>
{
    public async ValueTask<Result> HandleAsync(
        RegisterExternalAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (await pendingSignIns.FindAsync() is not { } pending
            || providers.Find(pending.Identity.Provider) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.NotPending);
        }

        var locale = SupportedLocale.Create(command.Locale);

        if (locale.IsFailure)
        {
            return Result.Failure(locale.Error);
        }

        var identity = pending.Identity;
        var address = await addresses.ResolveAsync(identity.VerifiedEmail);

        if (address.BelongsToPasswordAccount)
        {
            return Result.Failure(ExternalSignInErrors.AccountExists);
        }

        var user = User.RegisterExternal(
            address.Email,
            command.DisplayName,
            locale.Value,
            command.BirthYear,
            timeProvider.GetUtcNow());

        if (user.IsFailure)
        {
            return await RefuseAsync(identity, user.Error);
        }

        context.UserLogins.Add(new IdentityUserLogin<Guid>
        {
            LoginProvider = identity.Provider,
            ProviderKey = identity.Subject,
            ProviderDisplayName = provider.DisplayName,
            UserId = user.Value.Id
        });

        var created = await CreateAsync(user.Value);

        if (created.Succeeded)
        {
            logger.LogInformation("Registered account {AccountId} with {Provider}.", user.Value.Id, identity.Provider);

            await pendingSignIns.EndAsync();
            await signInManager.SignInWithClaimsAsync(
                user.Value,
                new AuthenticationProperties { IssuedUtc = pending.SignedInAt },
                [new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Federated)]);

            return Result.Success();
        }

        if (!created.Errors.Any(error => error.IsAlreadyRegistered()))
        {
            return Result.Failure(created.Errors.First().ToError());
        }

        context.ChangeTracker.Clear();

        if (await userManager.FindByLoginAsync(identity.Provider, identity.Subject) is not { } registered)
        {
            return Result.Failure(ExternalSignInErrors.AccountExists);
        }

        await pendingSignIns.EndAsync();

        logger.LogInformation(
            "Refused a {Provider} sign-up: account {AccountId} already signs in with it.",
            identity.Provider,
            registered.Id);

        return Result.Failure(ExternalSignInErrors.AlreadyLinked);
    }

    private async Task<Result> RefuseAsync(ExternalIdentity identity, Error error)
    {
        if (error == User.SchoolAccountRequired)
        {
            await pendingSignIns.EndAsync();

            logger.LogInformation("Refused a {Provider} sign-up that needs a school account.", identity.Provider);
        }

        return Result.Failure(error);
    }

    private async Task<IdentityResult> CreateAsync(User user)
    {
        try
        {
            return await userManager.CreateAsync(user);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return IdentityResult.Failed(userManager.ErrorDescriber.DuplicateUserName(user.UserName ?? string.Empty));
        }
    }
}
