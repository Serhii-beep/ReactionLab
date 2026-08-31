namespace ReactionLab.CatalogBuilder;

internal sealed class ReactionNaming(IReadOnlyDictionary<string, string> substanceNames)
{
    private static readonly HashSet<string> Metals = new(StringComparer.Ordinal)
    {
        "Li", "Na", "K", "Ba", "Ca", "Mg", "Al", "Zn", "Fe", "Ni", "Sn", "Pb", "Cu", "Ag", "Au"
    };

    public string Name(Candidate candidate)
    {
        var reactants = candidate.Reactants.Select(participant => participant.Formula).ToList();

        return candidate.Family switch
        {
            "combustion" => $"{CombustionWord(candidate.Variant)} combustion of {Lower(Fuel(reactants))}",
            "synthesis" => Synthesis(candidate, reactants),
            "decomposition" => $"Thermal decomposition of {Lower(reactants[0])}",
            "neutralization" => $"Neutralization of {Lower(Acid(reactants))} with {Lower(Other(reactants, Acid(reactants)))}",
            "precipitation" => $"{Display(reactants[0])} and {Lower(reactants[1])}",
            _ => Replacement(candidate, reactants)
        };
    }

    public string Description(Candidate candidate)
    {
        var reactants = candidate.Reactants.Select(participant => participant.Formula).ToList();
        var solid = candidate.Products.FirstOrDefault(p => p.Phase == "Solid")?.Formula;

        return candidate.Rule switch
        {
            "combustion.complete" => "Burns in a plentiful supply of oxygen, giving the fully oxidized products.",
            "combustion.incomplete" => "Burns in a limited supply of oxygen, so carbon monoxide forms instead of carbon dioxide.",
            "combustion.sooting" => "Burns in a very limited supply of oxygen, depositing carbon as soot.",
            "synthesis.metalAndNonMetal" => $"The two elements combine directly to give {Lower(candidate.Products[0].Formula)}.",
            "synthesis.oxideAndWater" => $"The oxide takes up water to give {Lower(candidate.Products[0].Formula)}.",
            "decomposition.carbonate" => "Heating drives off carbon dioxide and leaves the metal oxide.",
            "decomposition.hydroxide" => "Heating drives off water and leaves the metal oxide.",
            "decomposition.hydrogencarbonate" => "Heating gives the carbonate, water and carbon dioxide.",
            "neutralization.saltAndWater" => $"Acid and base react in solution to give {Lower(candidate.Products[0].Formula)} and water.",
            "singleReplacement.metalAndAcid" => $"{Display(Metal(reactants))} displaces hydrogen from the acid, releasing hydrogen gas.",
            "singleReplacement.metalAndColdWater" => $"{Display(Metal(reactants))} reacts with cold water, giving the hydroxide and hydrogen.",
            "singleReplacement.metalAndSteam" => $"{Display(Metal(reactants))} reacts with steam, giving the oxide and hydrogen.",
            "singleReplacement.metalAndSalt" => $"{Display(Metal(reactants))} sits higher in the activity series, so it takes the other metal's place in solution.",
            _ => solid is null
                ? "Mixing the two solutions gives a new pair of ionic compounds."
                : $"Mixing the two solutions brings {Lower(solid)} out of solution as a solid."
        };
    }

    private string Synthesis(Candidate candidate, List<string> reactants)
    {
        if (string.Equals(candidate.Variant, "oxideAndWater", StringComparison.Ordinal))
        {
            var oxide = reactants.First(formula => !string.Equals(formula, "H2O", StringComparison.Ordinal));

            return $"{Display(oxide)} reacts with water";
        }

        var metal = Metal(reactants);

        return $"{Display(metal)} reacts with {Lower(Other(reactants, metal))}";
    }

    private string Replacement(Candidate candidate, List<string> reactants)
    {
        var metal = Metal(reactants);

        return candidate.Variant switch
        {
            "metalAndColdWater" => $"{Display(metal)} and cold water",
            "metalAndSteam" => $"{Display(metal)} and steam",
            "metalAndAcid" => $"{Display(metal)} and {Lower(Other(reactants, metal))}",
            _ => $"{Display(metal)} displaces {Lower(Displaced(candidate))} from {Lower(Other(reactants, metal))}"
        };
    }

    private static string CombustionWord(string variant) => variant switch
    {
        "complete" => "Complete",
        "incomplete" => "Incomplete",
        _ => "Sooting"
    };

    private static string Fuel(List<string> reactants) =>
        reactants.First(formula => formula is not ("O2" or "O3"));

    private static string Metal(List<string> reactants) =>
        reactants.FirstOrDefault(Metals.Contains) ?? reactants[0];

    private static string Acid(List<string> reactants) =>
        reactants.FirstOrDefault(formula =>
            formula.Length > 1 && formula[0] == 'H' && !char.IsAsciiLetterLower(formula[1]))
        ?? reactants[0];

    private static string Other(List<string> reactants, string exclude) =>
        reactants.First(formula => !string.Equals(formula, exclude, StringComparison.Ordinal));

    private static string Displaced(Candidate candidate) =>
        candidate.Products.Select(participant => participant.Formula).FirstOrDefault(Metals.Contains)
        ?? candidate.Products[^1].Formula;

    private string Display(string formula) =>
        substanceNames.TryGetValue(formula, out var name) ? name : formula;

    private string Lower(string formula)
    {
        var name = Display(formula);

        return char.IsUpper(name[0]) && (name.Length == 1 || !char.IsUpper(name[1]))
            ? char.ToLowerInvariant(name[0]) + name[1..]
            : name;
    }
}
