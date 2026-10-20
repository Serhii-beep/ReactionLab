using ReactionLab.Identity.Api.Features.OpenId;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn;

internal static class SignInPageRedirects
{
    private const string ErrorParameter = "error";
    private const string ReturnUrlParameter = "returnUrl";

    public static string WithError(string errorCode, string returnUrl) =>
        AuthorizationEndpointHandler.SignInPagePath
        + QueryString.Create(ErrorParameter, errorCode).Add(ReturnUrlParameter, returnUrl);
}
