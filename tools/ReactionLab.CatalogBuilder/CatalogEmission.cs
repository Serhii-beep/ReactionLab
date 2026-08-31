using System.Text.Json;
using ReactionLab.Domain.Reactions;
using ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.CatalogBuilder;

internal static class CatalogEmission
{
    private static readonly JsonSerializerOptions CuratedOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task RunAsync(string root)
    {
        var sources = Path.Combine(root, "data", "sources");
        var output = Path.Combine(root, "data", "catalog", "v1");

        var substances = ReadSubstances(Path.Combine(output, "substances.jsonl"));
        var curated = Curated(await ReadCuratedAsync(Path.Combine(sources, "reactions.json")));
        var taken = curated.Select(Signature).ToHashSet(StringComparer.Ordinal);

        var naming = new ReactionNaming(substances);
        var validation = new CatalogValidation(substances.Keys.ToHashSet(StringComparer.Ordinal));

        var records = new List<ReactionRecord>(curated);
        var rejections = new List<string>();
        var rediscovered = 0;

        foreach (var candidate in ReadCandidates(Path.Combine(root, "data", "candidates", "reactions.jsonl")))
        {
            if (taken.Contains(candidate.Signature))
            {
                rediscovered++;

                continue;
            }

            if (!validation.Accepts(candidate, out var rejection))
            {
                rejections.Add($"{candidate.Signature}: {rejection}");

                continue;
            }

            records.Add(Generated(candidate, naming));
            taken.Add(candidate.Signature);
        }

        var ordered = records.OrderBy(record => record.Key, StringComparer.Ordinal).ToList();

        await CatalogJson.WriteLinesAsync(
            Path.Combine(output, "reactions.jsonl"), ordered, CancellationToken.None);

        foreach (var rejection in rejections.Order(StringComparer.Ordinal))
        {
            Console.WriteLine($"   rejected  {rejection}");
        }

        Console.WriteLine($"   curated                {curated.Count,6}");
        Console.WriteLine($"   rediscovered by a rule {rediscovered,6}");
        Console.WriteLine($"   generated              {ordered.Count - curated.Count,6}");
        Console.WriteLine($"   rejected               {rejections.Count,6}");
        Console.WriteLine($"reactions: {ordered.Count} written to {output}");
    }

    private static ReactionRecord Generated(Candidate candidate, ReactionNaming naming)
    {
        var name = naming.Name(candidate);

        return new ReactionRecord
        {
            Key = Slug(name),
            Type = ReactionClassification.Type(candidate),
            Difficulty = ReactionClassification.Difficulty(candidate),
            IsReversible = false,
            EnthalpyKjPerMol = candidate.EnthalpyKjPerMol,
            ActivationEnergyKjPerMol = candidate.ActivationEnergyKjPerMol,
            TemperatureK = candidate.MinimumKelvin,
            EffectPreset = ReactionClassification.EffectPreset(candidate),
            AnimationDurationMs = ReactionClassification.AnimationDurationMs,
            Rule = candidate.Rule,
            Confidence = candidate.Confidence,
            Tags = ReactionClassification.Tags(candidate),
            Participants =
            [
                .. candidate.Reactants.Select(p => new ReactionRecord.ParticipantRecord(
                    p.Formula, "Reactant", p.Coefficient, p.Phase, null)),
                .. candidate.Products.Select(p => new ReactionRecord.ParticipantRecord(
                    p.Formula, "Product", p.Coefficient, p.Phase, null))
            ],
            Translations = new Dictionary<string, ReactionRecord.ReactionText>
            {
                ["en"] = new(name, naming.Description(candidate), null, null, null)
            }
        };
    }

    private static List<ReactionRecord> Curated(List<SourceReaction> raw) =>
    [
        .. raw.Select(reaction => new ReactionRecord
        {
            Key = Slug(reaction.Name),
            Type = reaction.ReactionType,
            Difficulty = reaction.DifficultyLevel,
            EnthalpyKjPerMol = reaction.EnthalpyChange,
            ActivationEnergyKjPerMol = reaction.ActivationEnergy,
            TemperatureK = reaction.RequiredTemperature,
            Catalyst = reaction.CatalystInfo,
            EffectPreset = reaction.EffectPreset,
            AnimationDurationMs = reaction.AnimationDurationMs,
            Rule = ReactionProvenance.CuratedRule,
            Confidence = 1.0m,
            Tags = reaction.Tags,
            Participants = (reaction.Participants ?? [])
                .Select(p => new ReactionRecord.ParticipantRecord(
                    p.MoleculeFormula, p.Role, p.Coefficient, p.State, p.Substance))
                .ToList(),
            Translations = new Dictionary<string, ReactionRecord.ReactionText>
            {
                ["en"] = new(reaction.Name, reaction.Description, reaction.Mechanism,
                    reaction.SafetyWarnings, reaction.RealWorldExamples)
            }
        })
    ];

    private static string Signature(ReactionRecord record)
    {
        var side = (string role) => string.Join(
            " + ",
            record.Participants
                .Where(p => string.Equals(p.Role, role, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Formula)
                .Order(StringComparer.Ordinal));

        return $"{side("Reactant")} -> {side("Product")}";
    }

    private static Dictionary<string, string> ReadSubstances(string path)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(path))
        {
            var record = JsonDocument.Parse(line).RootElement;
            var formula = record.GetProperty("formula").GetString()!;

            names.TryAdd(
                formula,
                record.GetProperty("translations").GetProperty("en").GetProperty("name").GetString()!);
        }

        return names;
    }

    private static IEnumerable<Candidate> ReadCandidates(string path) =>
        File.ReadLines(path).Select(line => JsonSerializer.Deserialize<Candidate>(line, CatalogJson.Options)!);

    private static async Task<List<SourceReaction>> ReadCuratedAsync(string path)
    {
        await using var stream = File.OpenRead(path);

        return (await JsonSerializer.DeserializeAsync<List<SourceReaction>>(stream, CuratedOptions))!;
    }

    private static string Slug(string value) =>
        string.Join(
            '-',
            new string([.. value.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ')])
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
