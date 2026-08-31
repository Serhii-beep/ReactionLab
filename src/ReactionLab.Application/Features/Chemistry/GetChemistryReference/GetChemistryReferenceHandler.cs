using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Features.Chemistry.GetChemistryReference;

public sealed class GetChemistryReferenceHandler(IAppDbContext context, HybridCache cache)
    : IQueryHandler<GetChemistryReferenceQuery, string>
{
    public async ValueTask<Result<string>> HandleAsync(
        GetChemistryReferenceQuery query,
        CancellationToken cancellationToken)
    {
        var document = await cache.GetOrCreateAsync(
            CacheKeys.ChemistryReference,
            async token => await ChemistryQueries.DocumentAsync(
                context.ChemistryReferences.AsNoTracking(), token),
            CachePolicies.Reference,
            [CacheTags.ChemistryReference],
            cancellationToken);

        return Result.Success(document);
    }
}
