using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence;

internal sealed class ElementSymbolFiltering : ISubstanceFiltering
{
    public IQueryable<Substance> Containing(IQueryable<Substance> source, ElementSymbol element)
    {
        string[] wanted = [element.Value];

        return source.Where(substance =>
            wanted.All(symbol => EF.Property<string[]>(substance, PersistenceColumns.ElementSymbols).Contains(symbol)));
    }
}
