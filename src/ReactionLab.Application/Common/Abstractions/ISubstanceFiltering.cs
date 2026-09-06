using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Common.Abstractions;

public interface ISubstanceFiltering
{
    IQueryable<Substance> Containing(IQueryable<Substance> source, ElementSymbol element);
}
