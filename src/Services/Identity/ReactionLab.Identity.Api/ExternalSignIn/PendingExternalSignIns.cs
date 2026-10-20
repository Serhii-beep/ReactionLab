using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class PendingExternalSignIns(IHttpContextAccessor httpContextAccessor)
{
    private const string ProviderClaim = "provider";
    private const string SubjectClaim = "sub";
    private const string VerifiedEmailClaim = "email";
    private const string NameClaim = "name";

    public Task StartAsync(ExternalIdentity identity, string returnUrl)
    {
        List<Claim> claims = [new(ProviderClaim, identity.Provider), new(SubjectClaim, identity.Subject)];

        if (identity.VerifiedEmail is { } email)
        {
            claims.Add(new Claim(VerifiedEmailClaim, email));
        }

        if (identity.Name is { } name)
        {
            claims.Add(new Claim(NameClaim, name));
        }

        return HttpContext.SignInAsync(
            IdentityConstants.ExternalScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ExternalScheme)),
            new AuthenticationProperties { RedirectUri = returnUrl });
    }

    public async Task<PendingExternalSignIn?> FindAsync()
    {
        var pending = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (!pending.Succeeded
            || pending.Properties.IssuedUtc is not { } signedInAt
            || pending.Principal.FindFirstValue(ProviderClaim) is not { } provider
            || pending.Principal.FindFirstValue(SubjectClaim) is not { } subject)
        {
            return null;
        }

        var identity = new ExternalIdentity(
            provider,
            subject,
            pending.Principal.FindFirstValue(VerifiedEmailClaim),
            pending.Principal.FindFirstValue(NameClaim));

        return new PendingExternalSignIn(
            identity,
            LocalReturnUrls.OrDefault(pending.Properties.RedirectUri),
            signedInAt);
    }

    public Task EndAsync() => HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No request is in progress.");
}
