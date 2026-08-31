using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Reference;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Common.Abstractions;

public interface IAppDbContext
{
    DbSet<Element> Elements { get; }

    DbSet<Substance> Substances { get; }

    DbSet<Reaction> Reactions { get; }

    DbSet<ChemistryReference> ChemistryReferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
