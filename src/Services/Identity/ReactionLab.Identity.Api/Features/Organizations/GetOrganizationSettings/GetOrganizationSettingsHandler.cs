using ReactionLab.Identity.Api.Features.Members;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.GetOrganizationSettings;

internal sealed class GetOrganizationSettingsHandler(SchoolAdminTransaction adminTransaction, AppDbContext context)
    : IQueryHandler<GetOrganizationSettingsQuery, OrganizationSettingsResponse>
{
    public async ValueTask<Result<OrganizationSettingsResponse>> HandleAsync(
        GetOrganizationSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.AuthorizeAsync(
            query.ActorId,
            query.OrganizationId,
            SchoolAdminRules.Admin,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<OrganizationSettingsResponse>(access.Error);
        }

        return await context.FindOrganizationAsync(query.OrganizationId, cancellationToken) is { } organization
            ? Result.Success(OrganizationSettingsResponse.From(organization))
            : Result.Failure<OrganizationSettingsResponse>(OrganizationErrors.NotFound);
    }
}
