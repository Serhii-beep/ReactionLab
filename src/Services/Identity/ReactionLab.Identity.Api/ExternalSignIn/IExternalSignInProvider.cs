using System.Security.Claims;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal interface IExternalSignInProvider
{
    string Name { get; }

    string DisplayName { get; }

    void Register(OpenIddictClientBuilder client, Uri redirectUri);

    ExternalIdentity? ReadIdentity(ClaimsPrincipal principal);
}
