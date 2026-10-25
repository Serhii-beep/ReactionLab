using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.GetOrganization;

internal sealed class GetOrganizationHandler(AppDbContext context)
    : IQueryHandler<GetOrganizationQuery, OrganizationResponse>
{
    public async ValueTask<Result<OrganizationResponse>> HandleAsync(
        GetOrganizationQuery query,
        CancellationToken cancellationToken) =>
        await context.FindOrganizationAsync(query.OrganizationId, cancellationToken) is { } organization
            ? Result.Success(OrganizationResponse.From(organization))
            : Result.Failure<OrganizationResponse>(OrganizationErrors.NotFound);
}
