using ReactionLab.Catalog.Domain.Reactions;

namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface IReactionMatching
{
    IQueryable<Reaction> PossibleWith(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds, ReactantMatch match);

    IOrderedQueryable<Reaction> NearestFirst(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds);
}
