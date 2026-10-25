using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.RevokePlatformRole;

internal sealed class RevokePlatformRoleHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<RevokePlatformRoleHandler> logger) : ICommandHandler<RevokePlatformRoleCommand>
{
    public async ValueTask<Result> HandleAsync(RevokePlatformRoleCommand command, CancellationToken cancellationToken)
    {
        if (!PlatformRoleNames.IsKnown(command.Role))
        {
            return Result.Failure(PlatformRoleErrors.Unknown);
        }

        if (await FindRefusalAsync(command, cancellationToken) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AdminId,
            command.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not revoke {Role} from account {AccountId}: {ErrorCode}.",
                command.AdminSessionId,
                command.Role,
                command.AccountId,
                proof.Error.Code);

            return proof;
        }

        var revoked = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => RevokeAsync(command));

        if (revoked.IsFailure)
        {
            return Result.Failure(revoked.Error);
        }

        logger.LogInformation(
            "Admin {AdminId} revoked {Role} from account {AccountId} in session {SessionId}.",
            command.AdminId,
            command.Role,
            command.AccountId,
            command.AdminSessionId);

        return Result.Success();
    }

    private async Task<Result<bool>> RevokeAsync(RevokePlatformRoleCommand command)
    {
        if (await FindRefusalAsync(command, CancellationToken.None) is { } refusal)
        {
            return Result.Failure<bool>(refusal);
        }

        var revoked = await context.RevokePlatformRoleAsync(
            command.AccountId,
            command.Role,
            PlatformRoleChangeAuthor.Admin(command.AdminId, command.AdminSessionId));

        return revoked == 1 ? Result.Success(true) : Result.Failure<bool>(PlatformRoleErrors.NotGranted);
    }

    private async Task<Error?> FindRefusalAsync(RevokePlatformRoleCommand command, CancellationToken cancellationToken)
    {
        if (!await context.HoldsPlatformRoleAsync(command.AccountId, command.Role, cancellationToken))
        {
            return PlatformRoleErrors.NotGranted;
        }

        return command.Role == PlatformRoleNames.Admin
            && await context.CountPlatformAdminsWithEstablishedPasswordAsync(
                command.AccountId,
                timeProvider.GetUtcNow(),
                cancellationToken) == 0
                ? PlatformRoleErrors.LastAdmin
                : null;
    }
}
