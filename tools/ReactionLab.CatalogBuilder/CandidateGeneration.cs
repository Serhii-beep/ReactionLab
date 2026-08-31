using System.Text.Encodings.Web;
using System.Text.Json;
using ReactionLab.Chemistry.Generation;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;

namespace ReactionLab.CatalogBuilder;

internal static class CandidateGeneration
{
    public static void Run(string root)
    {
        var sources = Path.Combine(root, "data", "sources");
        var table = ReferenceData.ReadIons(Path.Combine(sources, "ions.json"));
        var series = ReferenceData.ReadActivitySeries(Path.Combine(sources, "activity-series.json"));

        var generator = new ReactionGenerator(new ReactionPredictor(
        [
            new CombustionRule(),
            new NeutralizationRule(table),
            new PrecipitationRule(table),
            new SingleReplacementRule(series, table),
            new SynthesisRule(series, table),
            new DecompositionRule(table)
        ]));

        var substances = ReadSubstances(Path.Combine(root, "data", "catalog", "v1", "substances.jsonl"));
        Console.WriteLine($"Generating from {substances.Count} substances.");

        var candidates = generator.From(substances);
        var output = Path.Combine(root, "data", "candidates");
        Directory.CreateDirectory(output);

        using var writer = new StreamWriter(Path.Combine(output, "reactions.jsonl"));

        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        foreach (var candidate in candidates)
        {
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                signature = candidate.Signature,
                rule = candidate.Rule,
                confidence = candidate.Confidence,
                minimumKelvin = candidate.MinimumKelvin,
                reactants = candidate.Reactants.Select(p => new { formula = p.Formula, coefficient = p.Coefficient }),
                products = candidate.Products.Select(p => new { formula = p.Formula, coefficient = p.Coefficient })
            }, options));
        }

        foreach (var family in candidates.GroupBy(c => c.Rule.Split('.')[0]).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"   {family.Key,-20}{family.Count(),6}");
        }

        Console.WriteLine($"{candidates.Count} candidates written to {output}.");
    }

    private static List<Reagent> ReadSubstances(string path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var substances = new List<Reagent>();

        foreach (var line in File.ReadLines(path))
        {
            var formula = JsonDocument.Parse(line).RootElement.GetProperty("formula").GetString()!;

            if (seen.Add(formula) && Reagent.TryCreate(formula, out var reagent))
            {
                substances.Add(reagent);
            }
        }

        return substances;
    }
}
