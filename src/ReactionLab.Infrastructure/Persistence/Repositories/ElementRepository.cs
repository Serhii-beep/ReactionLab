using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Infrastructure.Persistence.Repositories;

public class ElementRepository : Repository<Element>, IElementRepository
{
    public ElementRepository(ReactionLabDbContext context) : base(context)
    {

    }

    public async Task<Element?> GetByAtomicNumberAsync(int atomicNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.AtomicNumber == atomicNumber, cancellationToken);
    }

    public async Task<Element?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Symbol.ToLower() == symbol.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Element>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Name.ToLower().Contains(searchTerm.ToLower()) || e.Symbol.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync(cancellationToken);
    }
}