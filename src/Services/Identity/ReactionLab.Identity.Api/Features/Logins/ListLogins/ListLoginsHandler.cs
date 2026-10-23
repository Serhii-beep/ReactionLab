using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Logins.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Logins.ListLogins;

internal sealed class ListLoginsHandler(
    AppDbContext context,
    ExternalSignInProviders providers,
    TimeProvider timeProvider) : IQueryHandler<ListLoginsQuery, LoginsResponse>
{
    public async ValueTask<Result<LoginsResponse>> HandleAsync(
        ListLoginsQuery query,
        CancellationToken cancellationToken)
    {
        if (await context.FindSignInMethodsAsync(query.AccountId, cancellationToken) is not { } methods)
        {
            return Result.Failure<LoginsResponse>(AccountErrors.NotFound);
        }

        return Result.Success(new LoginsResponse(
            methods.HasPassword,
            methods.IsPasswordOnProbation(timeProvider.GetUtcNow()) ? methods.PasswordProbationEndsAt : null,
            [.. providers.All.Select(provider => Describe(provider, methods))]));
    }

    private static LoginProviderResponse Describe(IExternalSignInProvider provider, SignInMethods methods) =>
        methods.Logins.FirstOrDefault(login => login.Provider == provider.Name) is { } login
            ? new LoginProviderResponse(provider.Name, provider.DisplayName, true, login.ConnectedAt)
            : new LoginProviderResponse(provider.Name, provider.DisplayName, false, null);
}
