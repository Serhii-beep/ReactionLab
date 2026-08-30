namespace ReactionLab.Chemistry.Ions;

public sealed class IonTable(
    IReadOnlyList<Ion> cations,
    IReadOnlyList<Ion> anions,
    IReadOnlyList<SolubilityRule> solubilityRules)
{
    public IReadOnlyList<Ion> Cations { get; } = cations;

    public IReadOnlyList<Ion> Anions { get; } = anions;

    public bool TrySplit(string formula, out Ion cation, out Ion anion)
    {
        foreach (var candidateCation in Cations)
        {
            foreach (var candidateAnion in Anions)
            {
                var written = IonicFormula.Combine(
                    candidateCation.Formula, candidateCation.Magnitude,
                    candidateAnion.Formula, candidateAnion.Magnitude);

                if (string.Equals(written, formula, StringComparison.Ordinal))
                {
                    cation = candidateCation;
                    anion = candidateAnion;
                    return true;
                }
            }
        }

        cation = default;
        anion = default;
        return false;
    }

    public Solubility SolubilityOf(Ion cation, Ion anion, out string rule)
    {
        foreach (var candidate in solubilityRules)
        {
            if (!Matches(candidate.Cations, cation.Formula) || !Matches(candidate.Anions, anion.Formula))
            {
                continue;
            }

            rule = candidate.Code;

            return Contains(candidate.ExceptCations, cation.Formula)
                ? Reversed(candidate.Solubility)
                : candidate.Solubility;
        }

        rule = "unmatched";
        return Solubility.Insoluble;
    }

    private static bool Matches(IReadOnlyList<string>? formulas, string formula) =>
        formulas is null || formulas.Count == 0 || Contains(formulas, formula);

    private static bool Contains(IReadOnlyList<string>? formulas, string formula) =>
        formulas is not null
        && formulas.Any(candidate => string.Equals(candidate, formula, StringComparison.Ordinal));

    private static Solubility Reversed(Solubility solubility) =>
        solubility == Solubility.Soluble ? Solubility.Insoluble : Solubility.Soluble;
}
