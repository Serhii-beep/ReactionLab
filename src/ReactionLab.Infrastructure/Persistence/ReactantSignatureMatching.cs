using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Infrastructure.Persistence;

internal sealed class ReactantSignatureMatching : IReactionMatching
{
    public IQueryable<Reaction> PossibleWith(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds)
    {
        var available = availableSubstanceIds as Guid[] ?? [.. availableSubstanceIds];

        return source.Where(reaction =>
            EF.Property<Guid[]>(reaction, PersistenceColumns.ReactantSignature)
                .All(id => available.Contains(id)));
    }
}
