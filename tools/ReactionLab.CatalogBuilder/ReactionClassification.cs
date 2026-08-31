using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.CatalogBuilder;

internal static class ReactionClassification
{
    public const int AnimationDurationMs = 3000;

    public static string Type(Candidate candidate) => candidate.Family switch
    {
        "combustion" => "Combustion",
        "synthesis" => "Synthesis",
        "decomposition" => "Decomposition",
        "neutralization" => "Neutralization",
        "precipitation" => "Precipitation",
        _ => "SingleReplacement"
    };

    public static int Difficulty(Candidate candidate)
    {
        var elements = DistinctElements(candidate) >= 4 ? 1 : 0;
        var balancing = candidate.All.Max(participant => participant.Coefficient) >= 4 ? 1 : 0;

        return Math.Clamp(1 + elements + balancing, 1, 3);
    }

    public static List<string> Tags(Candidate candidate)
    {
        var tags = new List<string> { FamilyTag(candidate.Family) };

        if (candidate.EnthalpyKjPerMol is { } enthalpy)
        {
            tags.Add(enthalpy < 0 ? "exothermic" : "endothermic");
        }

        tags.Add(Difficulty(candidate) switch
        {
            1 => "beginner",
            2 => "intermediate",
            _ => "advanced"
        });

        return tags;
    }

    public static string EffectPreset(Candidate candidate) => candidate.Rule switch
    {
        "synthesis.oxideAndWater" or "singleReplacement.metalAndSteam" => "steam-burst",
        "synthesis.metalAndNonMetal" => "ionic-crystallization",
        "singleReplacement.metalAndAcid" => "vigorous-bubbling",
        "singleReplacement.metalAndColdWater" => "skittering-flame",
        "singleReplacement.metalAndSalt" => "color-change",
        _ => candidate.Family switch
        {
            "combustion" => "blue-flame",
            "decomposition" => "furnace-heat",
            "neutralization" => "warm-glow",
            _ => "white-precipitate"
        }
    };

    private static string FamilyTag(string family) =>
        string.Equals(family, "singleReplacement", StringComparison.Ordinal) ? "single-replacement" : family;

    private static int DistinctElements(Candidate candidate)
    {
        var symbols = new HashSet<string>(StringComparer.Ordinal);

        foreach (var formula in candidate.All
            .Select(participant => participant.Formula)
            .Distinct(StringComparer.Ordinal))
        {
            if (!FormulaParser.TryParse(formula, out var composition, out _))
            {
                continue;
            }

            foreach (var (symbol, _) in composition.Elements)
            {
                symbols.Add(symbol);
            }
        }

        return symbols.Count;
    }
}
