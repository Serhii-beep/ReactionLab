using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeSession;

internal sealed class RevokeSessionHandler(
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<RevokeSessionHandler> logger) : ICommandHandler<RevokeSessionCommand>
{
    public async ValueTask<Result> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (command.SessionId != command.CurrentSessionId
            && !SessionPolicy.IsRecentSignIn(command.SignedInAt, timeProvider.GetUtcNow()))
        {
            logger.LogInformation(
                "Session {ActingSessionId} needs a recent sign-in to end session {SessionId}.",
                command.CurrentSessionId,
                command.SessionId);

            return Result.Failure(SessionErrors.RecentSignInRequired);
        }

        var revocation = await context.RevokeSessionAsync(
            command.AccountId.ToString(),
            command.CurrentSessionId,
            command.SessionId);

        if (!revocation.ActingSessionValid)
        {
            return Result.Failure(SessionErrors.Ended);
        }

        if (!revocation.SessionFound)
        {
            return Result.Failure(SessionErrors.NotFound);
        }

        if (revocation.SessionEnded)
        {
            logger.LogInformation(
                "Session {ActingSessionId} ended session {SessionId}.",
                command.CurrentSessionId,
                command.SessionId);
        }

        return Result.Success();
    }
}
