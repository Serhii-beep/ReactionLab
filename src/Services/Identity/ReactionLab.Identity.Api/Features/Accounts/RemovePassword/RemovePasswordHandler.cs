using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.RemovePassword;

internal sealed class RemovePasswordHandler(
    ExternalSignInProviders providers,
    RecentSignInCheck recentSignInCheck,
    AppDbContext context,
    SecurityStampRotation securityStampRotation,
    ILogger<RemovePasswordHandler> logger) : ICommandHandler<RemovePasswordCommand>
{
    public async ValueTask<Result> HandleAsync(RemovePasswordCommand command, CancellationToken cancellationToken)
    {
        if (await FindRefusalAsync(command.AccountId, cancellationToken) is { } refusal)
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
                "Session {SessionId} may not remove the password of account {AccountId}: {ErrorCode}.",
                command.CurrentSessionId,
                command.AccountId,
                proof.Error.Code);

            return proof;
        }

        var rotation = await securityStampRotation.RemovePasswordAsync(
            command.AccountId,
            command.CurrentSessionId,
            providers.Names);

        if (!rotation.AccountMatched)
        {
            return Result.Failure(
                await FindRefusalAsync(command.AccountId, cancellationToken) ?? AccountErrors.LastSignInMethod);
        }

        if (!rotation.ActingSessionValid)
        {
            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {SessionId} removed the password of account {AccountId}, ending {SessionCount} other session(s).",
            command.CurrentSessionId,
            command.AccountId,
            rotation.EndedSessionCount);

        return Result.Success();
    }

    private async Task<Error?> FindRefusalAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await context.FindSignInMethodsAsync(accountId, cancellationToken) is not { } methods)
        {
            return AccountErrors.NotFound;
        }

        if (methods.IsSchoolAccount)
        {
            return AccountErrors.ManagedBySchool;
        }

        if (!methods.HasPassword)
        {
            return AccountErrors.PasswordNotSet;
        }

        return methods.CountUsableLogins(providers.Names) > 0 ? null : AccountErrors.LastSignInMethod;
    }
}
