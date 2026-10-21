using ReactionLab.Identity.Api.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;

internal sealed class RevokeOtherSessionsHandler(
    RecentSignInCheck recentSignInCheck,
    SecurityStampRotation securityStampRotation,
    ILogger<RevokeOtherSessionsHandler> logger) : ICommandHandler<RevokeOtherSessionsCommand>
{
    public async ValueTask<Result> HandleAsync(RevokeOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        var recentSignIn = await recentSignInCheck.CheckAsync(
            command.AccountId,
            command.CurrentSessionId,
            command.SignIn,
            cancellationToken);

        if (recentSignIn.IsFailure)
        {
            logger.LogInformation(
                "Session {ActingSessionId} may not end the other sessions of account {AccountId}: {ErrorCode}.",
                command.CurrentSessionId,
                command.AccountId,
                recentSignIn.Error.Code);

            return recentSignIn;
        }

        var rotation = await securityStampRotation.RotateAsync(command.AccountId, command.CurrentSessionId);

        if (!rotation.AccountMatched || !rotation.ActingSessionValid)
        {
            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {ActingSessionId} ended {SessionCount} other session(s) of account {AccountId}.",
            command.CurrentSessionId,
            rotation.EndedSessionCount,
            command.AccountId);

        return Result.Success();
    }
}
