using System.Collections.Concurrent;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;
using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.CatalogBuilder;

internal static class CatalogVerification
{
    private const int ReportedFailures = 20;

    private const int BalanceBatchSize = 20_000;

    public static async Task<int> RunAsync(string root, string[] args)
    {
        var build = CommandLineOptions.ValueOf(args, "--build");
        var directory = build is null
            ? Path.Combine(root, "data", "catalog", "v1")
            : Path.Combine(root, "data", "builds", build);
        var files = await CatalogBuildFiles.OpenAsync(directory, CancellationToken.None);
        var mapper = new CatalogRecordMapper();
        var failures = new ConcurrentQueue<string>();
        var elements = 0;
        var substances = 0;
        var reactions = 0;
        var structureKeys = new Dictionary<string, string>(StringComparer.Ordinal);

        await foreach (var record in files.ElementsAsync(CancellationToken.None))
        {
            elements += mapper.MapElement(record) is null ? 0 : 1;
        }

        await foreach (var record in files.SubstancesAsync(CancellationToken.None))
        {
            if (mapper.MapSubstance(record) is not { } substance)
            {
                continue;
            }

            substances++;

            if (StructureKeys.Of(substance.Structure) is { } key
                && !structureKeys.TryAdd(key, substance.Formula.Value)
                && record.PubChemCid is null)
            {
                failures.Enqueue($"{substance.Formula.Value}: its structure key is shared with {structureKeys[key]}");
            }
        }

        var batch = new List<ReactionRecord>(BalanceBatchSize);

        await foreach (var record in files.ReactionsAsync(CancellationToken.None))
        {
            if (mapper.MapReaction(record) is null)
            {
                continue;
            }

            reactions++;
            batch.Add(record);

            if (batch.Count == BalanceBatchSize)
            {
                CheckBalances(batch, failures);
                batch.Clear();
            }
        }

        CheckBalances(batch, failures);

        foreach (var rejection in mapper.Rejections)
        {
            failures.Enqueue($"rejected: {rejection}");
        }

        foreach (var failure in failures.Take(ReportedFailures))
        {
            Console.WriteLine(failure);
        }

        Console.WriteLine(
            $"Verified {elements} element(s), {substances} substance(s) with {structureKeys.Count} structure key(s), " +
            $"and {reactions} reaction(s) in {directory}: {failures.Count} failure(s).");

        return failures.IsEmpty ? 0 : 1;
    }

    private static void CheckBalances(List<ReactionRecord> batch, ConcurrentQueue<string> failures) =>
        Parallel.ForEach(batch, record =>
        {
            if (BalanceFailure(record) is { } failure)
            {
                failures.Enqueue($"{record.Key}: {failure}");
            }
        });

    private static string? BalanceFailure(ReactionRecord record)
    {
        var reactants = record.Participants.Where(participant => participant.Role == "Reactant").ToList();
        var products = record.Participants.Where(participant => participant.Role == "Product").ToList();
        var compositions = new List<ChemicalComposition>();

        foreach (var participant in reactants.Concat(products))
        {
            if (!FormulaParser.TryParse(participant.Formula, out var composition, out _))
            {
                return $"{participant.Formula} does not parse";
            }

            compositions.Add(composition);
        }

        if (!EquationBalancer.TryBalance(compositions[..reactants.Count], compositions[reactants.Count..], out var balanced, out var error))
        {
            return $"the engine cannot balance it ({error})";
        }

        int[] stated = [.. reactants.Concat(products).Select(participant => participant.Coefficient)];
        int[] engine = [.. balanced.ReactantCoefficients.Concat(balanced.ProductCoefficients)];
        var divisor = stated.Aggregate(GreatestCommonDivisor);

        return stated.Select(coefficient => coefficient / divisor).SequenceEqual(engine)
            ? null
            : $"its coefficients {string.Join(' ', stated)} differ from the engine's {string.Join(' ', engine)}";
    }

    private static int GreatestCommonDivisor(int first, int second) =>
        second == 0 ? first : GreatestCommonDivisor(second, first % second);
}
