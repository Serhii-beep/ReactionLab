using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Reference;
using ReactionLab.Catalog.Domain.Releases;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface IAppDbContext
{
    DbSet<Element> Elements { get; }

    DbSet<Substance> Substances { get; }

    DbSet<Reaction> Reactions { get; }

    DbSet<ChemistryReference> ChemistryReferences { get; }

    DbSet<CatalogRelease> CatalogReleases { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
