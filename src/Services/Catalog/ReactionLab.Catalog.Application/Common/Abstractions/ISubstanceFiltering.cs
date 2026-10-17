using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface ISubstanceFiltering
{
    IQueryable<Substance> Containing(IQueryable<Substance> source, ElementSymbol element);
}
