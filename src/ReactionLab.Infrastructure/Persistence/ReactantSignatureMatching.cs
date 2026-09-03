using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Infrastructure.Persistence;

internal sealed class ReactantSignatureMatching : IReactionMatching
{
    public IQueryable<Reaction> PossibleWith(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds, ReactantMatch match)
    {
        var available = availableSubstanceIds as Guid[] ?? [.. availableSubstanceIds];

        var candidates = source.Where(reaction =>
            EF.Property<Guid[]>(reaction, PersistenceColumns.ReactantSignature).Length > 0);

        return match == ReactantMatch.Complete
            ? candidates.Where(reaction =>
                EF.Property<Guid[]>(reaction, PersistenceColumns.ReactantSignature)
                    .All(id => available.Contains(id)))
            : candidates.Where(reaction =>
                EF.Property<Guid[]>(reaction, PersistenceColumns.ReactantSignature)
                    .Any(id => available.Contains(id)));
    }

    public IOrderedQueryable<Reaction> NearestFirst(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds)
    {
        var available = availableSubstanceIds as Guid[] ?? [.. availableSubstanceIds];

        return source
            .OrderBy(reaction => EF.Property<Guid[]>(reaction, PersistenceColumns.ReactantSignature)
                .Count(id => !available.Contains(id)))
            .ThenBy(reaction => reaction.Id);
    }
}
