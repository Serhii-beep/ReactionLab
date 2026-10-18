using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Chemistry.Constitution;

public static class ConstitutionKey
{
    public const int Length = 32;

    private const string Hydrogen = "H";

    public static string Of(IReadOnlyList<string> symbols, IReadOnlyList<AtomBond> bonds)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(bonds);

        var heavyAtoms = HeavyAtomGraph.Of(symbols, bonds);
        var description = new StringBuilder(HillFormulaOf(symbols));

        foreach (var signatures in heavyAtoms.Refinements())
        {
            description.Append('|').AppendJoin(';', signatures);
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(description.ToString())))[..Length];
    }

    private static string HillFormulaOf(IReadOnlyList<string> symbols)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var symbol in symbols)
        {
            counts[symbol] = counts.GetValueOrDefault(symbol) + 1;
        }

        return new ChemicalComposition(counts, charge: 0).Hill;
    }

    private sealed class HeavyAtomGraph
    {
        private readonly string[] _symbols;
        private readonly int[] _hydrogens;
        private readonly List<int>[] _neighbors;

        private HeavyAtomGraph(string[] symbols, int[] hydrogens, List<int>[] neighbors)
        {
            _symbols = symbols;
            _hydrogens = hydrogens;
            _neighbors = neighbors;
        }

        public static HeavyAtomGraph Of(IReadOnlyList<string> symbols, IReadOnlyList<AtomBond> bonds)
        {
            var heavyPositionByAtom = new Dictionary<int, int>();
            var heavySymbols = new List<string>();

            for (var atom = 0; atom < symbols.Count; atom++)
            {
                if (!string.Equals(symbols[atom], Hydrogen, StringComparison.Ordinal))
                {
                    heavyPositionByAtom[atom] = heavySymbols.Count;
                    heavySymbols.Add(symbols[atom]);
                }
            }

            var hydrogens = new int[heavySymbols.Count];
            var neighbors = heavySymbols.Select(_ => new List<int>()).ToArray();

            foreach (var bond in bonds)
            {
                var fromIsHeavy = heavyPositionByAtom.TryGetValue(bond.From, out var from);
                var toIsHeavy = heavyPositionByAtom.TryGetValue(bond.To, out var to);

                if (fromIsHeavy && toIsHeavy)
                {
                    neighbors[from].Add(to);
                    neighbors[to].Add(from);
                }
                else if (fromIsHeavy)
                {
                    hydrogens[from]++;
                }
                else if (toIsHeavy)
                {
                    hydrogens[to]++;
                }
            }

            return new HeavyAtomGraph([.. heavySymbols], hydrogens, neighbors);
        }

        public IEnumerable<IReadOnlyList<string>> Refinements()
        {
            var labels = _symbols
                .Select((symbol, atom) => $"{symbol}{_neighbors[atom].Count}H{_hydrogens[atom]}")
                .ToArray();
            var distinctLabels = labels.Distinct(StringComparer.Ordinal).Count();

            yield return Sorted(labels);

            for (var round = 0; round < _symbols.Length; round++)
            {
                var signatures = labels
                    .Select((label, atom) => $"{label}({string.Join(',', _neighbors[atom].Select(neighbor => labels[neighbor]).Order(StringComparer.Ordinal))})")
                    .ToArray();

                var rankBySignature = signatures.Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .Select((signature, rank) => (signature, rank))
                    .ToDictionary(pair => pair.signature, pair => pair.rank.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal);

                yield return Sorted(signatures);

                labels = [.. signatures.Select(signature => rankBySignature[signature])];

                if (rankBySignature.Count == distinctLabels)
                {
                    yield break;
                }

                distinctLabels = rankBySignature.Count;
            }
        }

        private static string[] Sorted(IEnumerable<string> values) => [.. values.Order(StringComparer.Ordinal)];
    }
}
