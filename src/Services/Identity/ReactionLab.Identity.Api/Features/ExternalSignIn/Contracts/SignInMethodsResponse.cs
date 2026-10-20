namespace ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;

internal sealed record SignInMethodsResponse(IReadOnlyList<ExternalSignInProviderResponse> Providers);
