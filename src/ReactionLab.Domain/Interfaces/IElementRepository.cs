using ReactionLab.Domain.Entities;

namespace ReactionLab.Domain.Interfaces;

public interface IElementRepository : IRepository<Element>
{
    Task<Element?> GetByAtomicNumberAsync(int atomicNumber, CancellationToken cancellationToken = default);

    Task<Element?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Element>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}
