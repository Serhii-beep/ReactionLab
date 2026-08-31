using System.Globalization;
using System.Numerics;
using System.Text.Json;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.CatalogBuilder;

internal static class GeometryAccuracy
{
    private static readonly double[] Buckets = [0.10d, 0.25d, 0.50d, 1.00d];

    public static void Run(string root)
    {
        var table = ReferenceData.ReadAtomicGeometry(Path.Combine(root, "data", "sources", "reference", "atomic-geometry.json"));

        var rigid = new List<(string Formula, double Deviation)>();
        var flexible = new List<(string Formula, double Deviation)>();
        var refused = new List<string>();

        foreach (var line in File.ReadLines(Path.Combine(root, "data", "catalog", "v1", "substances.jsonl")))
        {
            var record = JsonDocument.Parse(line).RootElement;

            if (!record.TryGetProperty("structure", out var structure))
            {
                continue;
            }

            var formula = record.GetProperty("formula").GetString()!;
            var symbols = structure.GetProperty("atoms").EnumerateArray()
                .Select(atom => atom.GetProperty("symbol").GetString()!).ToList();

            var reference = structure.GetProperty("atoms").EnumerateArray()
                .Select(atom => new Vector3(
                    (float)atom.GetProperty("x").GetDouble(),
                    (float)atom.GetProperty("y").GetDouble(),
                    (float)atom.GetProperty("z").GetDouble())).ToList();

            var bonds = structure.GetProperty("bonds").EnumerateArray()
                .Select(bond => new AtomBond(
                    bond.GetProperty("from").GetInt32(),
                    bond.GetProperty("to").GetInt32(),
                    Order(bond.GetProperty("type").GetString()!))).ToList();

            var heavy = Enumerable.Range(0, symbols.Count)
                .Where(index => !string.Equals(symbols[index], "H", StringComparison.Ordinal)).ToList();

            if (heavy.Count < 2)
            {
                continue;
            }

            if (!MoleculeAssembler.TryAssemble(
                [.. symbols.Select(symbol => new SkeletalAtom(symbol))], bonds, table,
                out var generated, out var error))
            {
                refused.Add($"{formula}: {error}");

                continue;
            }

            if (!Superposition.TryBestRootMeanSquareDeviation(
                [.. heavy.Select(index => reference[index])],
                [.. heavy.Select(index => generated[index])],
                Interchangeable(symbols, bonds, heavy),
                out var deviation))
            {
                refused.Add($"{formula}: superposition refused");

                continue;
            }

            (Rotatable(symbols, bonds) == 0 ? rigid : flexible).Add((formula, deviation));
        }

        Report("rigid", rigid);
        Report("flexible", flexible);

        foreach (var refusal in refused.Order(StringComparer.Ordinal))
        {
            Console.WriteLine($"   refused  {refusal}");
        }
    }

    private static void Report(string label, List<(string Formula, double Deviation)> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        var ordered = results.OrderBy(result => result.Deviation).ToList();
        var mean = ordered.Average(result => result.Deviation);
        var median = ordered[ordered.Count / 2].Deviation;
        var buckets = string.Join(", ", Buckets.Select(limit => $"{ordered.Count(result => result.Deviation <= limit)} within {Format(limit)}"));

        Console.WriteLine(
            $"{label}: {ordered.Count} molecules, mean {Format(mean)} A, median {Format(median)} A, " +
            $"{buckets}, worst {ordered[^1].Formula} at {Format(ordered[^1].Deviation)} A");

        foreach (var (formula, deviation) in ordered.TakeLast(5))
        {
            Console.WriteLine($"      {formula,-12}{Format(deviation)}");
        }
    }

    private static string Format(double value) =>
        value.ToString("0.000", CultureInfo.InvariantCulture);

    private static int Order(string type) => type switch
    {
        "double" => 2,
        "triple" => 3,
        "aromatic" => 4,
        _ => 1
    };

    private static int Rotatable(List<string> symbols, List<AtomBond> bonds)
    {
        var neighbours = symbols.Select(_ => new List<int>()).ToList();

        foreach (var bond in bonds)
        {
            neighbours[bond.From].Add(bond.To);
            neighbours[bond.To].Add(bond.From);
        }

        var heavyDegree = Enumerable.Range(0, symbols.Count)
            .Select(index => neighbours[index]
                .Count(other => !string.Equals(symbols[other], "H", StringComparison.Ordinal)))
            .ToList();

        return bonds.Count(bond =>
            bond.Order == 1
            && !string.Equals(symbols[bond.From], "H", StringComparison.Ordinal)
            && !string.Equals(symbols[bond.To], "H", StringComparison.Ordinal)
            && heavyDegree[bond.From] >= 2
            && heavyDegree[bond.To] >= 2
            && !InRing(neighbours, bond));
    }

    private static bool InRing(List<List<int>> neighbours, AtomBond bond)
    {
        var seen = new HashSet<int> { bond.From };
        var stack = new Stack<int>([bond.From]);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var next in neighbours[current])
            {
                if ((current == bond.From && next == bond.To) || (current == bond.To && next == bond.From))
                {
                    continue;
                }

                if (next == bond.To)
                {
                    return true;
                }

                if (seen.Add(next))
                {
                    stack.Push(next);
                }
            }
        }

        return false;
    }

    private static List<IReadOnlyList<int>> Interchangeable(
        List<string> symbols, List<AtomBond> bonds, List<int> heavy)
    {
        var neighbours = symbols.Select(_ => new List<(int Other, int Order)>()).ToList();

        foreach (var bond in bonds)
        {
            neighbours[bond.From].Add((bond.To, bond.Order));
            neighbours[bond.To].Add((bond.From, bond.Order));
        }

        var groups = new Dictionary<(string Symbol, int Parent, int Order), List<int>>();

        for (var slot = 0; slot < heavy.Count; slot++)
        {
            var attachments = neighbours[heavy[slot]]
                .Where(link => !string.Equals(symbols[link.Other], "H", StringComparison.Ordinal))
                .ToList();

            if (attachments.Count != 1)
            {
                continue;
            }

            var key = (symbols[heavy[slot]], attachments[0].Other, attachments[0].Order);

            if (!groups.TryGetValue(key, out var members))
            {
                groups[key] = members = [];
            }

            members.Add(slot);
        }

        return [.. groups.Values.Where(members => members.Count > 1)];
    }
}
