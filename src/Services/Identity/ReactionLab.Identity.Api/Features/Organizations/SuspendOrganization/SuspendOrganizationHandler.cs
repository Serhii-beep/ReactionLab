using ReactionLab.Identity.Api.Features.PlatformRoles;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.SuspendOrganization;

internal sealed class SuspendOrganizationHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformAdminTransaction adminTransaction,
    AppDbContext context,
    ILogger<SuspendOrganizationHandler> logger) : ICommandHandler<SuspendOrganizationCommand>
{
    public async ValueTask<Result> HandleAsync(SuspendOrganizationCommand command, CancellationToken cancellationToken)
    {
        if (await context.FindOrganizationAsync(command.OrganizationId, cancellationToken) is null)
        {
            return Result.Failure(OrganizationErrors.NotFound);
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AdminId,
            command.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not suspend organization {OrganizationId}: {ErrorCode}.",
                command.AdminSessionId,
                command.OrganizationId,
                proof.Error.Code);

            return proof;
        }

        var suspended = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => SuspendAsync(command));

        if (suspended.IsFailure)
        {
            return Result.Failure(suspended.Error);
        }

        if (suspended.Value > 0)
        {
            logger.LogInformation(
                "Admin {AdminId} suspended organization {OrganizationId} in session {SessionId}.",
                command.AdminId,
                command.OrganizationId,
                command.AdminSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<int>> SuspendAsync(SuspendOrganizationCommand command) =>
        Result.Success(await context.SuspendOrganizationAsync(
            command.OrganizationId,
            command.AdminId,
            command.AdminSessionId));
}
