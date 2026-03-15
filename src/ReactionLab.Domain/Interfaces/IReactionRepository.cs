using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Interfaces;

public interface IReactionRepository : IRepository<Reaction>
{
    Task<IReadOnlyList<Reaction>> GetByTypeAsync(ReactionType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reaction>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    Task<Reaction?> GetWithParticipantsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reaction>> FindByReactantsAsync(IEnumerable<Guid> elementIds, IEnumerable<Guid> moleculeIds, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Reaction> Items, bool HasMore)> FindAvailableReactionsAsync(
        IEnumerable<Guid> moleculeIds, 
        IEnumerable<Guid> elementIds, 
        string? searchTerm = null, 
        int pageSize = 20, 
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        CancellationToken cancellationToken = default);
}