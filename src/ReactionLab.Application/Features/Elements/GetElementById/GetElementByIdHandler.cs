using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.GetElementById;

public sealed class GetElementByIdHandler(IAppDbContext context)
    : IQueryHandler<GetElementByIdQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(
        GetElementByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = ElementId.From(query.Id);

        var row = await context.Elements
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.AtomicNumber,
                e.Symbol,
                e.Mass,
                e.Category,
                e.StateAtRoomTemperature,
                e.Position,
                e.DisplayColor,
                e.Electronegativity,
                e.Radii,
                e.MeltingPoint,
                e.BoilingPoint,
                e.ElectronConfiguration,
                e.Translations
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return ElementErrors.NotFound(query.Id);
        }

        var content = row.Translations.Resolve(query.Locale);

        return new ElementResponse(
            query.Id,
            row.AtomicNumber.Value,
            row.Symbol.Value,
            content.Name,
            row.Mass.Daltons,
            row.Category,
            row.StateAtRoomTemperature,
            row.Position.Period,
            row.Position.Group,
            row.DisplayColor.Value,
            row.Electronegativity?.Pauling,
            row.Radii?.CovalentPicometers,
            row.Radii?.VanDerWaalsPicometers,
            row.MeltingPoint?.Kelvin,
            row.BoilingPoint?.Kelvin,
            row.ElectronConfiguration,
            content.DiscoveryInfo,
            content.InterestingFacts);
    }
}
