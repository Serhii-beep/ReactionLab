using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Thermochemistry;

public sealed class PhaseResolution(IonTable table, IReadOnlyDictionary<string, Phase> standardStates)
{
    private static readonly string[] DissolvedFamilies = ["precipitation", "neutralization", "singleReplacement"];

    private static readonly string[] WaterLeavesAsSteam =
    [
        "singleReplacement.metalAndSteam",
        "decomposition.carbonate",
        "decomposition.hydroxide",
        "decomposition.hydrogencarbonate"
    ];

    public bool TryResolve(
        string rule,
        IReadOnlyList<string> reactants,
        IReadOnlyList<string> products,
        out PhaseAssignment assignment)
    {
        assignment = null!;

        if (!TryAll(rule, reactants, out var left) || !TryAll(rule, products, out var right))
        {
            return false;
        }

        assignment = new PhaseAssignment(left, right);

        return true;
    }

    public bool TryPhaseOf(string rule, string formula, out Phase phase)
    {
        if (Dissolves(rule))
        {
            if (table.TrySplit(formula, out var cation, out var anion))
            {
                phase = table.SolubilityOf(cation, anion, out _) == Solubility.Insoluble
                    ? Phase.Solid
                    : Phase.Aqueous;

                return true;
            }

            if (table.TryReadAcid(formula, out _))
            {
                phase = Phase.Aqueous;

                return true;
            }
        }

        if (string.Equals(formula, "H2O", StringComparison.Ordinal) && LeavesAsSteam(rule))
        {
            phase = Phase.Gas;

            return true;
        }

        return standardStates.TryGetValue(formula, out phase);
    }

    private bool TryAll(string rule, IReadOnlyList<string> species, out IReadOnlyList<Phase> phases)
    {
        var resolved = new Phase[species.Count];

        for (var i = 0; i < species.Count; i++)
        {
            if (!TryPhaseOf(rule, species[i], out resolved[i]))
            {
                phases = [];

                return false;
            }
        }

        phases = resolved;

        return true;
    }

    private static bool Dissolves(string rule) =>
        DissolvedFamilies.Any(family => rule.StartsWith(family, StringComparison.Ordinal));

    private static bool LeavesAsSteam(string rule) =>
        WaterLeavesAsSteam.Any(candidate => string.Equals(candidate, rule, StringComparison.Ordinal));
}
