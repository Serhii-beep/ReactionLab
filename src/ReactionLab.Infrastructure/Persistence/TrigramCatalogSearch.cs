using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;

namespace ReactionLab.Infrastructure.Persistence;

internal sealed class TrigramCatalogSearch : ICatalogSearch
{
    private const string Escape = @"\";

    public IOrderedQueryable<TEntity> Matching<TEntity>(IQueryable<TEntity> source, string term)
        where TEntity : class
    {
        var pattern = $"%{EscapeWildcards(term)}%";

        return source
            .Where(entity =>
                EF.Functions.ILike(
                    EF.Property<string>(entity, PersistenceColumns.SearchText), pattern, Escape)
                || EF.Functions.TrigramsAreWordSimilar(
                    term, EF.Property<string>(entity, PersistenceColumns.SearchText)))
            .OrderBy(entity =>
                EF.Functions.TrigramsWordSimilarityDistance(
                    term, EF.Property<string>(entity, PersistenceColumns.SearchText)));
    }

    private static string EscapeWildcards(string term) =>
        term.Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal);
}
