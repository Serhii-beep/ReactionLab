using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Infrastructure.Persistence.Repositories;

public class ReactionRepository : Repository<Reaction>, IReactionRepository
{
    public ReactionRepository(ReactionLabDbContext context) : base(context)
    {

    }

    public async Task<(IReadOnlyList<Reaction> Items, bool HasMore)> FindAvailableReactionsAsync(
        IEnumerable<Guid> moleculeIds,
        IEnumerable<Guid> elementIds,
        string? searchTerm = null,
        int pageSize = 20,
        DateTime? cursorCreatedAt = null,
        Guid? cursorId = null,
        CancellationToken cancellationToken = default)
    {
        var moleculeIdList = moleculeIds.ToList();
        var elementIdList = elementIds.ToList();

        if (moleculeIdList.Count == 0 && elementIdList.Count == 0)
        {
            return ([], false);
        }

        var query = _dbSet
            .Where(r => r.Participants.Any(p => p.Role == ParticipantRole.Reactant))
            .Where(r => r.Participants
                .Where(p => p.Role == ParticipantRole.Reactant)
                .All(p =>
                    (p.MoleculeId.HasValue && moleculeIdList.Contains(p.MoleculeId.Value)) ||
                    (p.ElementId.HasValue && elementIdList.Contains(p.ElementId.Value))));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(term) ||
                r.Equation.ToLower().Contains(term) ||
                (r.Category != null && r.Category.ToLower().Contains(term)));
        }

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(r =>
                r.CreatedAt < cursorCreatedAt.Value ||
                (r.CreatedAt == cursorCreatedAt.Value && r.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .Include(r => r.Participants)
                .ThenInclude(p => p.Molecule)
            .Include(r => r.Participants)
                .ThenInclude(p => p.Element)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;

        return (hasMore ? items.Take(pageSize).ToList() : items, hasMore);
    }

    public async Task<IReadOnlyList<Reaction>> FindByReactantsAsync(IEnumerable<Guid> elementIds, IEnumerable<Guid> moleculeIds, CancellationToken cancellationToken = default)
    {
        var elementIdList = elementIds.ToList();
        var moleculeIdList = moleculeIds.ToList();

        return await _dbSet
            .Include(r => r.Participants)
            .Where(r => r.Participants
                .Where(p => p.Role == ParticipantRole.Reactant)
                .All(p =>
                    (p.ElementId.HasValue && elementIdList.Contains(p.ElementId.Value)) ||
                    (p.MoleculeId.HasValue && moleculeIdList.Contains(p.MoleculeId.Value))))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reaction>> GetByTypeAsync(ReactionType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.ReactionType == type).ToListAsync(cancellationToken);
    }

    public async Task<Reaction?> GetWithParticipantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Participants)
                .ThenInclude(p => p.Element)
            .Include(r => r.Participants)
                .ThenInclude(p => p.Molecule)
            .Include(r => r.ReactionTags)
                .ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Reaction>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Name.ToLower().Contains(searchTerm.ToLower()) ||
                r.Equation.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync(cancellationToken);
    }
}