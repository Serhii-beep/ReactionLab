using System.Text.Encodings.Web;
using System.Text.Json;
using ReactionLab.Chemistry.Generation;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.CatalogBuilder;

internal static class CandidateGeneration
{
    public static void Run(string root)
    {
        var sources = Path.Combine(root, "data", "sources");
        var reference = Path.Combine(sources, "reference");
        var table = ReferenceData.ReadIons(Path.Combine(reference, "ions.json"));
        var series = ReferenceData.ReadActivitySeries(Path.Combine(reference, "activity-series.json"));

        var catalog = Path.Combine(root, "data", "catalog", "v1", "substances.jsonl");
        var substances = ReadSubstances(catalog);

        var thermodynamics = Path.Combine(reference, "thermodynamics.json");

        var generator = new ReactionGenerator(
            new ReactionPredictor(
            [
                new CombustionRule(),
                new NeutralizationRule(table),
                new PrecipitationRule(table),
                new SingleReplacementRule(series, table),
                new SynthesisRule(series, table),
                new DecompositionRule(table)
            ]),
            new PhaseResolution(table, ReadStandardStates(catalog)),
            new EnergyEstimator(
                new StandardStateTable(
                    ReferenceData.ReadSpeciesEnthalpy(thermodynamics),
                    ReferenceData.ReadAqueousIons(thermodynamics),
                    table),
                ReferenceData.ReadBarriers(thermodynamics)));

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
                enthalpyKjPerMol = candidate.EnthalpyKjPerMol,
                activationEnergyKjPerMol = candidate.ActivationEnergyKjPerMol,
                reactants = candidate.Reactants.Select(p => new { formula = p.Formula, coefficient = p.Coefficient, phase = p.Phase?.ToString() }),
                products = candidate.Products.Select(p => new { formula = p.Formula, coefficient = p.Coefficient, phase = p.Phase?.ToString() })
            }, options));
        }

        foreach (var family in candidates.GroupBy(c => c.Rule.Split('.')[0]).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"   {family.Key,-20}{family.Count(),6}");
        }

        var phased = candidates.Count(candidate =>
            candidate.Reactants.Concat(candidate.Products).All(participant => participant.Phase is not null));
        Console.WriteLine($"   {phased} of {candidates.Count} have a phase for every participant");

        var energetic = candidates.Count(candidate => candidate.EnthalpyKjPerMol is not null);
        Console.WriteLine($"   {energetic} of {candidates.Count} have a computed enthalpy");

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

    private static Dictionary<string, Phase> ReadStandardStates(string path)
    {
        var states = new Dictionary<string, Phase>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(path))
        {
            var record = JsonDocument.Parse(line).RootElement;
            var formula = record.GetProperty("formula").GetString()!;

            if (Enum.TryParse<Phase>(record.GetProperty("state").GetString(), ignoreCase: true, out var phase))
            {
                states[formula] = phase;
            }
        }

        return states;
    }
}
