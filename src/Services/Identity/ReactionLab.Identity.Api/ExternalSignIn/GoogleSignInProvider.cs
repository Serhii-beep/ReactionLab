using System.Security.Claims;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class GoogleSignInProvider(string clientId, string clientSecret) : IExternalSignInProvider
{
    public const string SettingsSection = "Google";

    private const string GmailSuffix = "@gmail.com";
    private const string HostedDomainClaim = "hd";

    public string Name => "google";

    public string DisplayName => "Google";

    public static GoogleSignInProvider? FromSettings(IConfigurationSection settings) =>
        settings["ClientId"] is { Length: > 0 } clientId && settings["ClientSecret"] is { Length: > 0 } clientSecret
            ? new GoogleSignInProvider(clientId, clientSecret)
            : null;

    public void Register(OpenIddictClientBuilder client, Uri redirectUri) =>
        client.UseWebProviders().AddGoogle(google => google
            .SetClientId(clientId)
            .SetClientSecret(clientSecret)
            .SetRedirectUri(redirectUri)
            .SetProviderName(Name)
            .SetProviderDisplayName(DisplayName)
            .SetPrompt(PromptValues.SelectAccount)
            .AddScopes(Scopes.Email, Scopes.Profile));

    public ExternalIdentity? ReadIdentity(ClaimsPrincipal principal) =>
        principal.GetClaim(Claims.Subject) is { Length: > 0 } subject
            ? new ExternalIdentity(Name, subject, ReadOwnedEmail(principal), principal.GetClaim(Claims.Name))
            : null;

    private static string? ReadOwnedEmail(ClaimsPrincipal principal) =>
        principal.GetClaims(Claims.Email) is [var email]
        && principal.GetClaims(Claims.EmailVerified) is { IsDefaultOrEmpty: false } verified
        && verified.All(IsTrue)
        && (email.EndsWith(GmailSuffix, StringComparison.OrdinalIgnoreCase)
            || principal.GetClaim(HostedDomainClaim) is { Length: > 0 })
            ? email
            : null;

    private static bool IsTrue(string value) => bool.TryParse(value, out var isTrue) && isTrue;
}
