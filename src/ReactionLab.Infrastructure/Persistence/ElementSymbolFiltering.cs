using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence;

internal sealed class ElementSymbolFiltering : ISubstanceFiltering
{
    public IQueryable<Substance> Containing(IQueryable<Substance> source, ElementSymbol element)
    {
        string[] wanted = [element.Value];

        return source.Where(substance =>
            wanted.All(symbol => EF.Property<string[]>(substance, PersistenceColumns.ElementSymbols).Contains(symbol)));
    }
}
