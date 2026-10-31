using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Client.AspNetCore;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Logins.CompleteLoginConnection;

internal sealed class CompleteLoginConnectionHandler(
    RecentSignInCheck recentSignInCheck,
    AppDbContext context,
    UserManager<User> userManager,
    ILogger<CompleteLoginConnectionHandler> logger)
{
    public async Task<IResult> HandleAsync(
        IExternalSignInProvider provider,
        AuthenticateResult authentication,
        LoginConnection connection)
    {
        var returnUrl = LocalReturnUrls.OrDefault(authentication.Properties?.RedirectUri);

        if (authentication.ReadIdentity(provider) is not { } identity)
        {
            var error = authentication.Properties?.GetString(OpenIddictClientAspNetCoreConstants.Properties.Error);

            logger.LogInformation(
                "Connecting {Provider} to account {AccountId} did not finish: {Error}.",
                provider.Name,
                connection.AccountId,
                error ?? "no usable identity");

            return Results.Redirect(
                LoginConnections.WithError(ExternalSignInErrors.CodeForProviderError(error), returnUrl));
        }

        var outcome = await ConnectAsync(provider, identity, connection);

        if (outcome.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} could not connect {Provider} to account {AccountId}: {ErrorCode}.",
                connection.SessionId,
                provider.Name,
                connection.AccountId,
                outcome.Error.Code);

            return Results.Redirect(LoginConnections.WithError(outcome.Error.Code, returnUrl));
        }

        if (outcome.Value == LoginConnectionOutcome.Added)
        {
            logger.LogInformation(
                "Session {SessionId} connected {Provider} to account {AccountId}.",
                connection.SessionId,
                provider.Name,
                connection.AccountId);
        }

        return Results.Redirect(returnUrl);
    }

    private async Task<Result<LoginConnectionOutcome>> ConnectAsync(
        IExternalSignInProvider provider,
        ExternalIdentity identity,
        LoginConnection connection)
    {
        var confirmedPassword = await recentSignInCheck.CheckConfirmedPasswordAsync(
            connection.AccountId,
            connection.SessionId,
            CancellationToken.None);

        if (confirmedPassword.IsFailure)
        {
            return Result.Failure<LoginConnectionOutcome>(confirmedPassword.Error);
        }

        if (await InsertAsync(provider, identity, connection) == 1)
        {
            return Result.Success(LoginConnectionOutcome.Added);
        }

        if (await userManager.FindByLoginAsync(provider.Name, identity.Subject) is { } linked)
        {
            return linked.Id == connection.AccountId
                ? Result.Success(LoginConnectionOutcome.AlreadyPresent)
                : Result.Failure<LoginConnectionOutcome>(LoginErrors.ConnectedElsewhere);
        }

        if (await context.FindSignInMethodsAsync(connection.AccountId, CancellationToken.None) is not { } methods)
        {
            return Result.Failure<LoginConnectionOutcome>(AccountErrors.NotFound);
        }

        if (methods.IsSchoolAccount)
        {
            return Result.Failure<LoginConnectionOutcome>(AccountErrors.ManagedBySchool);
        }

        return Result.Failure<LoginConnectionOutcome>(methods.Logins.Any(login => login.Provider == provider.Name)
            ? LoginErrors.AlreadyConnected
            : SessionErrors.Ended);
    }

    private Task<int> InsertAsync(
        IExternalSignInProvider provider,
        ExternalIdentity identity,
        LoginConnection connection) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var inserted = await context.ConnectLoginAsync(
                connection.AccountId,
                connection.SessionId,
                provider.Name,
                identity.Subject,
                provider.DisplayName);

            await transaction.CommitAsync(CancellationToken.None);

            return inserted;
        });
}
