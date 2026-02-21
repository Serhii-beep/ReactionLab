using ReactionLab.Domain.Entities;

namespace ReactionLab.Domain.Interfaces;

public interface IMoleculeRepository : IRepository<Molecule>
{
    Task<Molecule?> GetByFormulaAsync(string formula, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Molecule>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    Task<Molecule?> GetWithElementsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Molecule> Items, int TotalCount)> SearchAsync(string? searchTerm, int pageSize, DateTime? cursorCreatedAt = null, Guid? cursorId = null, CancellationToken cancellationToken = default);
}