using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Infrastructure.Persistence.Repositories;

public class MoleculeRepository : Repository<Molecule>, IMoleculeRepository
{
    public MoleculeRepository(ReactionLabDbContext context) : base(context)
    {

    }

    public async Task<Molecule?> GetByFormulaAsync(string formula, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Formula.ToLower() == formula.ToLower(), cancellationToken);
    }

    public async Task<Molecule?> GetWithElementsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(m => m.MoleculeElements).ThenInclude(me => me.Element).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Molecule> Items, int TotalCount)> SearchAsync(string? searchTerm, int pageSize, DateTime? cursorCreatedAt = null, Guid? cursorId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                m.Formula.ToLower().Contains(term) ||
                (m.IUPACName != null && m.IUPACName.ToLower().Contains(term)));
        }

        var totalCount = cursorCreatedAt.HasValue ? 0 : await query.CountAsync(cancellationToken);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(m =>
                m.CreatedAt < cursorCreatedAt.Value ||
                (m.CreatedAt == cursorCreatedAt.Value && m.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Molecule>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.Name.ToLower().Contains(searchTerm.ToLower()) || m.Formula.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync(cancellationToken);
    }


}
