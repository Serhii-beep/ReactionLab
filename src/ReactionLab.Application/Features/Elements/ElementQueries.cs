using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Elements;

internal static class ElementQueries
{
    public static async Task<ElementResponse?> FirstResponseAsync(
        IQueryable<Element> elements,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var row = await elements
            .Select(element => new
            {
                element.Id,
                element.AtomicNumber,
                element.Symbol,
                element.Mass,
                element.Category,
                element.StateAtRoomTemperature,
                element.Position,
                element.DisplayColor,
                element.Electronegativity,
                element.Radii,
                element.MeltingPoint,
                element.BoilingPoint,
                element.ElectronConfiguration,
                element.Translations
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var content = row.Translations.Resolve(locale);

        return new ElementResponse(
            row.Id.Value,
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

    public static async Task<IReadOnlyList<ElementSummaryResponse>> SummariesAsync(
        IQueryable<Element> elements,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var rows = await elements
            .Select(element => new
            {
                element.Id,
                element.AtomicNumber,
                element.Symbol,
                element.Mass,
                element.Category,
                element.Position,
                element.DisplayColor,
                element.Translations
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new ElementSummaryResponse(
            row.Id.Value,
            row.AtomicNumber.Value,
            row.Symbol.Value,
            row.Translations.Resolve(locale).Name,
            row.Mass.Daltons,
            row.Category,
            row.Position.Period,
            row.Position.Group,
            row.DisplayColor.Value));
    }
}
