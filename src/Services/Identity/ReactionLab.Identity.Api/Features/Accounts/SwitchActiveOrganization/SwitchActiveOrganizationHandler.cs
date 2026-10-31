using ReactionLab.Identity.Api.Features.Organizations;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SwitchActiveOrganization;

internal sealed class SwitchActiveOrganizationHandler(
    AppDbContext context,
    ILogger<SwitchActiveOrganizationHandler> logger) : ICommandHandler<SwitchActiveOrganizationCommand>
{
    public async ValueTask<Result> HandleAsync(
        SwitchActiveOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.OrganizationId is not { } organizationId)
        {
            return Result.Failure(AccountErrors.OrganizationRequired);
        }

        var organizations = await context.ListAccountOrganizationsAsync(command.AccountId, cancellationToken);

        if (organizations.SingleOrDefault(held => held.OrganizationId == organizationId) is not { } organization)
        {
            return Result.Failure(AccountErrors.OrganizationUnavailable);
        }

        if (organization.IsSuspended)
        {
            return Result.Failure(OrganizationErrors.Suspended);
        }

        var switched = await context.SetActiveOrganizationAsync(
            command.AccountId.ToString(),
            command.SessionId,
            organizationId);

        if (switched != 1)
        {
            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {SessionId} of account {AccountId} switched to organization {OrganizationId}.",
            command.SessionId,
            command.AccountId,
            organizationId);

        return Result.Success();
    }
}
