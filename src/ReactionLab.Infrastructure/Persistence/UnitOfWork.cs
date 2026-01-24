using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ReactionLabDbContext _context;
    private bool _disposed;

    public UnitOfWork(ReactionLabDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}