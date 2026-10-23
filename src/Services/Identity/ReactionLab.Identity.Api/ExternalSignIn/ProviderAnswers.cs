using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Persistence.Accounts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal static class ProviderAnswers
{
    public static ExternalIdentity? ReadIdentity(
        this AuthenticateResult authentication,
        IExternalSignInProvider provider) =>
        authentication.Succeeded
        && authentication.Principal.GetClaim(Claims.Private.ProviderName) == provider.Name
        && provider.ReadIdentity(authentication.Principal) is { } identity
        && identity.Subject.Length <= User.MaximumLoginKeyLength
            ? identity
            : null;
}
