using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using OpenIddict.Client.AspNetCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Logins.Contracts;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Features.Logins.ConnectLogin;

internal sealed class ConnectLoginHandler(
    ExternalSignInProviders providers,
    ProviderDiscovery discovery,
    RecentSignInCheck recentSignInCheck,
    AppDbContext context,
    ILogger<ConnectLoginHandler> logger)
{
    public async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ConnectLoginCommand command,
        CancellationToken cancellationToken)
    {
        if (providers.Find(command.Provider) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.UnknownProvider).ToHttpResult();
        }

        if (!LocalReturnUrls.TryNormalize(command.ReturnUrl, out var returnUrl))
        {
            return Result.Failure(ExternalSignInErrors.ReturnUrlInvalid).ToHttpResult();
        }

        if (await context.FindSignInMethodsAsync(command.AccountId, cancellationToken) is not { } methods)
        {
            return Result.Failure(AccountErrors.NotFound).ToHttpResult();
        }

        if (methods.Logins.Any(login => login.Provider == provider.Name))
        {
            return Result.Failure(LoginErrors.AlreadyConnected).ToHttpResult();
        }

        var confirmedPassword = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AccountId,
            command.CurrentSessionId,
            cancellationToken);

        if (confirmedPassword.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not connect {Provider} to account {AccountId}: {ErrorCode}.",
                command.CurrentSessionId,
                provider.Name,
                command.AccountId,
                confirmedPassword.Error.Code);

            return confirmedPassword.ToHttpResult();
        }

        if (discovery.IsPending(provider.Name))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        properties.Items[OpenIddictClientAspNetCoreConstants.Properties.ProviderName] = provider.Name;
        LoginConnections.Attach(properties, new LoginConnection(command.AccountId, command.CurrentSessionId));

        await httpContext.ChallengeAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme, properties);

        if (httpContext.Response.HasStarted)
        {
            return Results.Empty;
        }

        if (httpContext.Response.StatusCode != StatusCodes.Status302Found
            || httpContext.Response.Headers.Location.ToString() is not { Length: > 0 } location)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        httpContext.Response.Headers.Remove(HeaderNames.Location);

        return Results.Ok(new ConnectLoginResponse(location));
    }
}
