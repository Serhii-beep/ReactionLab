using ReactionLab.Domain.Reactions;

namespace ReactionLab.Application.Common.Abstractions;

public interface IReactionMatching
{
    IQueryable<Reaction> PossibleWith(IQueryable<Reaction> source, IReadOnlyCollection<Guid> availableSubstanceIds);
}
