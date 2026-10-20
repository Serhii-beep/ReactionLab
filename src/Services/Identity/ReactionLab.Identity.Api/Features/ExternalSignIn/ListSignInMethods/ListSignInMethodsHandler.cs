using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.ListSignInMethods;

internal sealed class ListSignInMethodsHandler(ExternalSignInProviders providers)
    : IQueryHandler<ListSignInMethodsQuery, SignInMethodsResponse>
{
    public ValueTask<Result<SignInMethodsResponse>> HandleAsync(
        ListSignInMethodsQuery query,
        CancellationToken cancellationToken)
    {
        var response = new SignInMethodsResponse([.. providers.All.Select(Describe)]);

        return ValueTask.FromResult(Result.Success(response));
    }

    private static ExternalSignInProviderResponse Describe(IExternalSignInProvider provider) =>
        new(provider.Name, provider.DisplayName);
}
