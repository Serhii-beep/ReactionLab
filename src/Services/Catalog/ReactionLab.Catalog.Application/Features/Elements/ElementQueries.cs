using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Features.Elements.Contracts;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements;

internal static class ElementQueries
{
    public static async Task<ElementResponse?> FirstResponseAsync(
        IQueryable<Element> elements,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var responses = await ResponsesAsync(elements.OrderBy(element => element.Id).Take(1), locale, cancellationToken);

        return responses.Count > 0 ? responses[0] : null;
    }

    public static async Task<IReadOnlyList<ElementResponse>> ResponsesAsync(
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
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row =>
        {
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
        });
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
                element.Electronegativity,
                element.Radii,
                element.MeltingPoint,
                element.BoilingPoint,
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
            row.DisplayColor.Value,
            row.Electronegativity?.Pauling,
            row.Radii?.CovalentPicometers,
            row.MeltingPoint?.Kelvin,
            row.BoilingPoint?.Kelvin));
    }
}
