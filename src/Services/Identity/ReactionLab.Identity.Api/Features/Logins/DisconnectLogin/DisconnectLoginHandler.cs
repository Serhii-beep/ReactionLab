using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Logins.DisconnectLogin;

internal sealed class DisconnectLoginHandler(
    ExternalSignInProviders providers,
    RecentSignInCheck recentSignInCheck,
    AppDbContext context,
    SecurityStampRotation securityStampRotation,
    TimeProvider timeProvider,
    ILogger<DisconnectLoginHandler> logger) : ICommandHandler<DisconnectLoginCommand>
{
    public async ValueTask<Result> HandleAsync(DisconnectLoginCommand command, CancellationToken cancellationToken)
    {
        if (providers.Find(command.Provider) is not { } provider)
        {
            return Result.Failure(ExternalSignInErrors.UnknownProvider);
        }

        var now = timeProvider.GetUtcNow();

        if (await FindRefusalAsync(command.AccountId, provider.Name, now, cancellationToken) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        var proof = await recentSignInCheck.CheckSignInMethodChangeAsync(
            command.AccountId,
            command.CurrentSessionId,
            command.SignIn,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not disconnect {Provider} from account {AccountId}: {ErrorCode}.",
                command.CurrentSessionId,
                provider.Name,
                command.AccountId,
                proof.Error.Code);

            return proof;
        }

        var rotation = await securityStampRotation.RemoveLoginAsync(
            command.AccountId,
            command.CurrentSessionId,
            provider.Name,
            providers.Names,
            now);

        if (!rotation.AccountMatched)
        {
            return Result.Failure(
                await FindRefusalAsync(command.AccountId, provider.Name, now, cancellationToken)
                    ?? AccountErrors.LastSignInMethod);
        }

        if (!rotation.ActingSessionValid)
        {
            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {SessionId} removed {Provider} from account {AccountId}, ending {SessionCount} other session(s).",
            command.CurrentSessionId,
            provider.Name,
            command.AccountId,
            rotation.EndedSessionCount);

        return Result.Success();
    }

    private async Task<Error?> FindRefusalAsync(
        Guid accountId,
        string providerName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await context.FindSignInMethodsAsync(accountId, cancellationToken) is not { } methods)
        {
            return AccountErrors.NotFound;
        }

        if (methods.Logins.All(login => login.Provider != providerName))
        {
            return LoginErrors.NotConnected;
        }

        if (methods.HasUsablePassword(now) || methods.CountUsableLogins(providers.Names) > 1)
        {
            return null;
        }

        return methods.IsPasswordOnProbation(now) && methods.PasswordProbationEndsAt is { } probationEndsAt
            ? AccountErrors.PasswordTooNew(probationEndsAt)
            : AccountErrors.LastSignInMethod;
    }
}
