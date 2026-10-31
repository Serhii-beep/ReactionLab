using ReactionLab.Identity.Api.Features.PlatformRoles;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.ReinstateOrganization;

internal sealed class ReinstateOrganizationHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformAdminTransaction adminTransaction,
    AppDbContext context,
    ILogger<ReinstateOrganizationHandler> logger) : ICommandHandler<ReinstateOrganizationCommand>
{
    public async ValueTask<Result> HandleAsync(
        ReinstateOrganizationCommand command,
        CancellationToken cancellationToken)
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
                "Session {SessionId} may not reinstate organization {OrganizationId}: {ErrorCode}.",
                command.AdminSessionId,
                command.OrganizationId,
                proof.Error.Code);

            return proof;
        }

        var reinstated = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => ReinstateAsync(command));

        if (reinstated.IsFailure)
        {
            return Result.Failure(reinstated.Error);
        }

        logger.LogInformation(
            "Admin {AdminId} reinstated organization {OrganizationId} in session {SessionId}.",
            command.AdminId,
            command.OrganizationId,
            command.AdminSessionId);

        return Result.Success();
    }

    private async Task<Result<int>> ReinstateAsync(ReinstateOrganizationCommand command)
    {
        var reinstated = await context.ReinstateOrganizationAsync(
            command.OrganizationId,
            command.AdminId,
            command.AdminSessionId);

        return reinstated > 0 ? Result.Success(reinstated) : Result.Failure<int>(OrganizationErrors.NotSuspended);
    }
}
