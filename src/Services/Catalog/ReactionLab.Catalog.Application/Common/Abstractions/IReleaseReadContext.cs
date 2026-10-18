using ReactionLab.Catalog.Application.Features.Releases.Snapshots;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface IReleaseReadContext
{
    Guid ReleaseId { get; }

    IQueryable<Substance> Substances { get; }

    IQueryable<Reaction> Reactions { get; }

    IQueryable<ReleaseSubstanceName> SubstanceNames { get; }

    IQueryable<ReleasePrefixTop> PrefixTops { get; }

    IQueryable<ReleaseWord> Words { get; }

    IQueryable<ReleaseSubstanceElement> SubstanceElements { get; }

    void UseRelease(Guid releaseId);
}
