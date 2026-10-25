using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal sealed class AuthorizationEndpointHandler(
    UserManager<User> userManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    SessionClaims sessionClaims,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<AuthorizationEndpointHandler> logger)
{
    public const string SignInPagePath = "/sign-in";

    private const string ReturnUrlParameter = "returnUrl";
    private const int MaximumDeviceLength = 256;

    public async Task<IResult> HandleAsync(HttpContext httpContext)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var signIn = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var user = signIn.Succeeded ? await userManager.GetUserAsync(signIn.Principal!) : null;

        if (user is null
            || user.OrganizationId is not null
            || signIn.Properties?.IssuedUtc is not { } signedInAt
            || !IsRecentEnough(signedInAt, request, httpContext))
        {
            return request.HasPromptValue(PromptValues.None)
                ? OpenIdResults.Forbid(Errors.LoginRequired, "The user must sign in.")
                : Results.Redirect(SignInPagePath + QueryString.Create(ReturnUrlParameter, ReturnUrl(httpContext.Request, request)));
        }

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The client application cannot be found.");

        var scopes = request.GetScopes();
        ImmutableArray<string> resources = [.. await scopeManager.ListResourcesAsync(scopes).ToArrayAsync()];

        var session = await authorizationManager.CreateAsync(DescribeSession(
            await applicationManager.GetIdAsync(application),
            user,
            signedInAt,
            scopes,
            httpContext));
        var sessionId = await authorizationManager.GetIdAsync(session)
            ?? throw new InvalidOperationException("The session has no identifier.");

        await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await RevokeOldestSessionsAsync(user, sessionId);

        var identity = await sessionClaims.CreateAsync(
            user,
            sessionId,
            signedInAt,
            [.. signIn.Principal!.FindAll(AuthenticationMethods.ClaimType).Select(claim => claim.Value)],
            scopes,
            resources);

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private bool IsRecentEnough(DateTimeOffset signedInAt, OpenIddictRequest request, HttpContext httpContext)
    {
        if (request.HasPromptValue(PromptValues.Login) || request.MaxAge <= 0)
        {
            return GetRequestedAt(httpContext) is { } requestedAt && signedInAt >= requestedAt;
        }

        return request.MaxAge is not { } maxAge || (timeProvider.GetUtcNow() - signedInAt).TotalSeconds <= maxAge;
    }

    private OpenIddictAuthorizationDescriptor DescribeSession(
        string? applicationId,
        User user,
        DateTimeOffset signedInAt,
        ImmutableArray<string> scopes,
        HttpContext httpContext)
    {
        var device = httpContext.Request.Headers.UserAgent.ToString();

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            CreationDate = timeProvider.GetUtcNow(),
            Status = Statuses.Valid,
            Subject = user.Id.ToString(),
            Type = AuthorizationTypes.AdHoc
        };

        descriptor.Scopes.UnionWith(scopes);
        descriptor.Properties[SessionProperties.Device] =
            JsonSerializer.SerializeToElement(device.Length > MaximumDeviceLength ? device[..MaximumDeviceLength] : device);
        descriptor.Properties[SessionProperties.Address] =
            JsonSerializer.SerializeToElement(httpContext.Connection.RemoteIpAddress?.ToString());
        descriptor.Properties[SessionProperties.SignedInAt] =
            JsonSerializer.SerializeToElement(signedInAt.ToUnixTimeSeconds());
        descriptor.Properties[SessionProperties.SecurityStampHash] =
            JsonSerializer.SerializeToElement(SessionSecurityStamps.Hash(user.SecurityStamp ?? string.Empty));

        return descriptor;
    }

    private async Task RevokeOldestSessionsAsync(User user, string sessionId)
    {
        var revoked = await context.RevokeOldestSessionsAsync(
            user.Id.ToString(),
            SessionPolicy.MaximumSessionsPerAccount);

        if (revoked > 0)
        {
            logger.LogInformation(
                "Session {SessionId} put account {AccountId} over the limit. Ended {SessionCount} older sign-in(s).",
                sessionId,
                user.Id,
                revoked);
        }
    }

    private static DateTimeOffset? GetRequestedAt(HttpContext httpContext) =>
        httpContext.Features.Get<OpenIddictServerAspNetCoreFeature>()?.Transaction
            ?.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName!)
            ?.RequestTokenPrincipal
            ?.GetCreationDate();

    private static string ReturnUrl(HttpRequest httpRequest, OpenIddictRequest request) =>
        httpRequest.PathBase + httpRequest.Path + QueryString.Create(
        [
            KeyValuePair.Create(Parameters.ClientId, request.ClientId),
            KeyValuePair.Create(Parameters.RequestUri, request.RequestUri)
        ]);
}
