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

    public async Task<IReadOnlyList<Molecule>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.Name.ToLower().Contains(searchTerm.ToLower()) || m.Formula.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync(cancellationToken);
    }
}