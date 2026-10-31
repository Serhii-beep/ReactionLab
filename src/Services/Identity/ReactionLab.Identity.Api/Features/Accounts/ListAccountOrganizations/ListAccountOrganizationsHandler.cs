using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.ListAccountOrganizations;

internal sealed class ListAccountOrganizationsHandler(AppDbContext context)
    : IQueryHandler<ListAccountOrganizationsQuery, IReadOnlyList<AccountOrganizationResponse>>
{
    public async ValueTask<Result<IReadOnlyList<AccountOrganizationResponse>>> HandleAsync(
        ListAccountOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var organizations = await context.ListAccountOrganizationsAsync(query.AccountId, cancellationToken);

        if (organizations.Count == 0)
        {
            return Result.Success<IReadOnlyList<AccountOrganizationResponse>>([]);
        }

        var preferredOrganizationId = await context.FindActiveOrganizationIdAsync(query.SessionId, cancellationToken);
        var active = await context.FindSchoolAccountAccessAsync(
            query.AccountId,
            preferredOrganizationId,
            cancellationToken);

        return Result.Success<IReadOnlyList<AccountOrganizationResponse>>(organizations
            .Select(organization => new AccountOrganizationResponse(
                organization.OrganizationId,
                organization.Kind,
                organization.Name,
                organization.Roles,
                organization.IsSuspended,
                organization.OrganizationId == active?.OrganizationId))
            .ToList());
    }
}
