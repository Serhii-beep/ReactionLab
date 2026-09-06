using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Infrastructure.Persistence;

public static class ProjectionBackfill
{
    public static async Task<int> FillElementSymbolsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var unfilled = await context.Substances
            .Where(substance => EF.Property<string[]>(substance, PersistenceColumns.ElementSymbols).Length == 0)
            .ToListAsync(cancellationToken);

        foreach (var substance in unfilled)
        {
            context.Entry(substance).State = EntityState.Modified;
        }

        await context.SaveChangesAsync(cancellationToken);

        return unfilled.Count;
    }
}
