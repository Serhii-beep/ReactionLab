using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances;

internal static class SubstanceQueries
{
    public static async Task<SubstanceResponse?> FirstResponseAsync(
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var row = await substances
            .Select(substance => new
            {
                substance.Id,
                substance.Formula,
                substance.Kind,
                substance.IsOrganic,
                substance.StateAtRoomTemperature,
                substance.Weight,
                substance.Structure,
                substance.Category,
                substance.Translations
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var content = row.Translations.Resolve(locale);

        return new SubstanceResponse(
            row.Id.Value,
            row.Formula.Value,
            row.Formula.Hill,
            content.Name,
            content.IupacName,
            content.Description,
            content.SafetyInformation,
            content.CommonNames,
            content.Uses,
            content.InterestingFacts,
            row.Kind,
            row.IsOrganic,
            row.StateAtRoomTemperature,
            row.Weight?.GramsPerMole,
            row.Category,
            ToStructure(row.Structure));
    }

    public static async Task<IReadOnlyList<SubstanceSummaryResponse>> SummariesAsync(
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var rows = await substances
            .Select(substance => new
            {
                substance.Id,
                substance.Formula,
                substance.Kind,
                substance.IsOrganic,
                substance.StateAtRoomTemperature,
                substance.Weight,
                substance.Category,
                substance.Translations
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new SubstanceSummaryResponse(
            row.Id.Value,
            row.Formula.Value,
            row.Translations.Resolve(locale).Name,
            row.Kind,
            row.IsOrganic,
            row.StateAtRoomTemperature,
            row.Weight?.GramsPerMole,
            row.Category));
    }

    private static MolecularStructureResponse? ToStructure(MolecularStructure? structure) =>
        structure is null
            ? null
            : new MolecularStructureResponse(
                [.. structure.Atoms.Select(atom =>
                    new AtomResponse(atom.Symbol.Value, atom.X, atom.Y, atom.Z))],
                [.. structure.Bonds.Select(bond =>
                    new BondResponse(bond.FromAtomIndex, bond.ToAtomIndex, bond.Type))]);
}
