using Microsoft.AspNetCore.Authentication;
using OpenIddict.Client.AspNetCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Http;
using ReactionLab.SharedKernel;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.StartExternalSignIn;

internal sealed class StartExternalSignInHandler(ExternalSignInProviders providers)
{
    public IResult Handle(string providerName, string? returnUrl)
    {
        if (providers.Find(providerName) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.UnknownProvider).ToHttpResult();
        }

        if (!LocalReturnUrls.TryNormalize(returnUrl, out var normalizedReturnUrl))
        {
            return Result.Failure(ExternalSignInErrors.ReturnUrlInvalid).ToHttpResult();
        }

        var properties = new AuthenticationProperties { RedirectUri = normalizedReturnUrl };
        properties.Items[OpenIddictClientAspNetCoreConstants.Properties.ProviderName] = provider.Name;

        return Results.Challenge(properties, [OpenIddictClientAspNetCoreDefaults.AuthenticationScheme]);
    }
}
