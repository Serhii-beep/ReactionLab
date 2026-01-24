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