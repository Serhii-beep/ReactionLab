using ReactionLab.Identity.Api.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;

internal sealed class RevokeOtherSessionsHandler(
    SecurityStampRotation securityStampRotation,
    TimeProvider timeProvider,
    ILogger<RevokeOtherSessionsHandler> logger) : ICommandHandler<RevokeOtherSessionsCommand>
{
    public async ValueTask<Result> HandleAsync(RevokeOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        if (!SessionPolicy.IsRecentSignIn(command.SignedInAt, timeProvider.GetUtcNow()))
        {
            logger.LogInformation(
                "Session {ActingSessionId} needs a recent sign-in to end the other sessions of account {AccountId}.",
                command.CurrentSessionId,
                command.AccountId);

            return Result.Failure(SessionErrors.RecentSignInRequired);
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
