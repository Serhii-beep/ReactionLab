using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions.RevokeSession;

internal sealed class RevokeSessionHandler(
    AppDbContext context,
    RecentSignInCheck recentSignInCheck,
    ILogger<RevokeSessionHandler> logger) : ICommandHandler<RevokeSessionCommand>
{
    public async ValueTask<Result> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (command.SessionId != command.CurrentSessionId)
        {
            var recentSignIn = await recentSignInCheck.CheckAsync(
                command.AccountId,
                command.CurrentSessionId,
                command.SignIn,
                cancellationToken);

            if (recentSignIn.IsFailure)
            {
                logger.LogInformation(
                    "Session {ActingSessionId} may not end session {SessionId}: {ErrorCode}.",
                    command.CurrentSessionId,
                    command.SessionId,
                    recentSignIn.Error.Code);

                return recentSignIn;
            }
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
