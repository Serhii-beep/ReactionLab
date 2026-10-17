using System.Globalization;
using System.Numerics;
using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Geometry;
using ReactionLab.Chemistry.Valence;

namespace ReactionLab.Chemistry.Browser;

internal static class SketchDescriber
{
    public static SketchDescription Describe(SketchRequest request, ChemistryReference reference)
    {
        List<string> symbols = [.. request.Atoms.Select(atom => atom.Symbol)];
        List<AtomBond> bonds = [.. request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order))];

        if (!IsWellFormed(symbols.Count, bonds))
        {
            throw new ArgumentException(
                "A bond names a missing atom, joins an atom to itself, repeats a bond or has an order outside 1 to 3.",
                nameof(request));
        }

        var valences = ValenceAnalysis.Analyze(
            symbols, [.. request.Atoms.Select(atom => atom.FormalCharge)], bonds, reference.AtomicGeometry, reference.Periods);
        var charge = valences.Sum(valence => valence.FormalCharge);
        var composition = CompositionOf(symbols, charge);
        var geometry = SketchAssembly.Of(symbols, valences, bonds, reference.AtomicGeometry);

        return new SketchDescription(
            [.. valences.Select(valence => new SketchAtomDescription(
                valence.FormalCharge, valence.FormalChargeInferred, valence.LonePairs, valence.OpenValence, valence.Status))],
            composition?.Hill,
            charge,
            composition is not null && MolarMass.TryCompute(composition, reference.AtomicMasses, out var gramsPerMole, out _)
                ? gramsPerMole
                : null,
            Flattened(geometry.PositionsAngstroms),
            geometry.OpenValenceOwners,
            Flattened(geometry.OpenValencePositionsAngstroms),
            geometry.Error,
            geometry is { PositionsAngstroms: { } positions, Fragments: 1 } ? SymmetryDescriber.Describe(symbols, positions) : null);
    }

    private static bool IsWellFormed(int atomCount, List<AtomBond> bonds)
    {
        var joined = new HashSet<(int, int)>();

        return bonds.TrueForAll(bond =>
            bond.From >= 0 && bond.From < atomCount
            && bond.To >= 0 && bond.To < atomCount
            && bond.From != bond.To
            && bond.Order is >= 1 and <= 3
            && joined.Add((Math.Min(bond.From, bond.To), Math.Max(bond.From, bond.To))));
    }

    private static ChemicalComposition? CompositionOf(List<string> symbols, int charge)
    {
        if (symbols.Count == 0)
        {
            return null;
        }

        var formula = string.Concat(symbols
            .GroupBy(symbol => symbol, StringComparer.Ordinal)
            .Select(group => group.Key + (group.Count() > 1 ? group.Count().ToString(CultureInfo.InvariantCulture) : string.Empty)))
            + ChargeSuffix(charge);

        return FormulaParser.TryParse(formula, out var composition, out _) ? composition : null;
    }

    private static string ChargeSuffix(int charge) => charge switch
    {
        0 => string.Empty,
        1 => "^+",
        -1 => "^-",
        > 0 => "^" + charge.ToString(CultureInfo.InvariantCulture) + "+",
        _ => "^" + (-charge).ToString(CultureInfo.InvariantCulture) + "-"
    };

    private static float[]? Flattened(IReadOnlyList<Vector3>? positions) =>
        positions?.SelectMany(position => new[] { position.X, position.Y, position.Z }).ToArray();
}
