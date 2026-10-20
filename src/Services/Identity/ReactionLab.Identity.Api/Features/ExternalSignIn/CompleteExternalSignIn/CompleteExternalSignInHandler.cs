using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Client.AspNetCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;
using ReactionLab.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.CompleteExternalSignIn;

internal sealed class CompleteExternalSignInHandler(
    ExternalSignInProviders providers,
    PendingExternalSignIns pendingSignIns,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<CompleteExternalSignInHandler> logger)
{
    public const string ExternalIdentityPagePath = "/sign-in/external";

    public async Task<IResult> HandleAsync(HttpContext httpContext, string providerName)
    {
        if (providers.Find(providerName) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.UnknownProvider).ToHttpResult();
        }

        var authentication = await httpContext.AuthenticateAsync(
            OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        var returnUrl = LocalReturnUrls.OrDefault(authentication.Properties?.RedirectUri);

        if (!authentication.Succeeded
            || authentication.Principal.GetClaim(Claims.Private.ProviderName) != provider.Name
            || provider.ReadIdentity(authentication.Principal) is not { } identity
            || identity.Subject.Length > User.MaximumLoginKeyLength)
        {
            return Refuse(provider, authentication, returnUrl);
        }

        var normalized = Normalize(identity);

        if (await userManager.FindByLoginAsync(normalized.Provider, normalized.Subject) is not { } user)
        {
            await pendingSignIns.StartAsync(normalized, returnUrl);

            logger.LogInformation(
                "A {Provider} account linked to no account was asked to sign up or link.",
                provider.Name);

            return Results.Redirect(ExternalIdentityPagePath);
        }

        if (!await signInManager.CanSignInAsync(user))
        {
            return Results.Redirect(SignInPageRedirects.WithError(ExternalSignInErrors.FailedCode, returnUrl));
        }

        await pendingSignIns.EndAsync();
        await signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: false,
            [new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Federated)]);

        logger.LogInformation("Account {AccountId} signed in with {Provider}.", user.Id, provider.Name);

        return Results.Redirect(returnUrl);
    }

    private IResult Refuse(IExternalSignInProvider provider, AuthenticateResult authentication, string returnUrl)
    {
        var error = authentication.Properties?.GetString(OpenIddictClientAspNetCoreConstants.Properties.Error);

        logger.LogInformation(
            "Sign-in with {Provider} did not finish: {Error}.",
            provider.Name,
            error ?? "no usable identity");

        var errorCode = error switch
        {
            Errors.AccessDenied => ExternalSignInErrors.CanceledCode,
            Errors.ServerError or Errors.TemporarilyUnavailable => ExternalSignInErrors.UnavailableCode,
            _ => ExternalSignInErrors.FailedCode
        };

        return Results.Redirect(SignInPageRedirects.WithError(errorCode, returnUrl));
    }

    private static ExternalIdentity Normalize(ExternalIdentity identity) =>
        identity with
        {
            VerifiedEmail = User.NormalizeEmail(identity.VerifiedEmail) is { IsSuccess: true } email
                ? email.Value
                : null,
            Name = User.NormalizeDisplayName(identity.Name) is { IsSuccess: true } name ? name.Value : null
        };
}
