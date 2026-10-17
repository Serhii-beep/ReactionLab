using System.Globalization;

namespace ReactionLab.Chemistry.Ions;

public sealed class IonTable(
    IReadOnlyList<Ion> cations,
    IReadOnlyList<Ion> anions,
    IReadOnlyList<SolubilityRule> solubilityRules,
    IonBehaviors behaviors)
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
        return Solubility.Unknown;
    }

    public bool TryReadAcid(string formula, out Ion anion)
    {
        anion = default;

        if (formula.Length < 2 || formula[0] != 'H' || char.IsAsciiLetterLower(formula[1]))
        {
            return false;
        }

        var index = 1;

        while (index < formula.Length && char.IsAsciiDigit(formula[index]))
        {
            index++;
        }

        var hydrogens = index == 1 ? 1 : int.Parse(formula[1..index], CultureInfo.InvariantCulture);
        var rest = formula[index..];

        if (rest.Length == 0 || rest is "O" or "OH")
        {
            return false;
        }

        return TryMatch(Anions, rest, hydrogens, out anion);
    }

    public bool TryReadBase(string formula, out Ion cation)
    {
        cation = default;
        string rest;
        int hydroxides;

        if (formula.EndsWith("OH", StringComparison.Ordinal))
        {
            rest = formula[..^2];
            hydroxides = 1;
        }
        else
        {
            var group = formula.LastIndexOf("(OH)", StringComparison.Ordinal);

            if (group < 0)
            {
                return false;
            }

            var count = formula[(group + 4)..];

            if (count.Length == 0 || !count.All(char.IsAsciiDigit))
            {
                return false;
            }

            rest = formula[..group];
            hydroxides = int.Parse(count, CultureInfo.InvariantCulture);
        }

        return TryMatch(Cations, rest, hydroxides, out cation);
    }

    public bool IsThermallyStable(Ion cation) => Holds(behaviors.ThermallyStableCations, cation.Formula);

    public bool IsOxidizing(Ion anion) => Holds(behaviors.OxidizingAnions, anion.Formula);

    public bool IsUnstableAcid(Ion anion) => Holds(behaviors.UnstableAcidAnions, anion.Formula);

    public bool Hydrolyzes(Ion anion) => Holds(behaviors.HydrolyzingAnions, anion.Formula);

    private static bool TryMatch(IReadOnlyList<Ion> known, string formula, int magnitude, out Ion found)
    {
        foreach (var candidate in known)
        {
            if (string.Equals(candidate.Formula, formula, StringComparison.Ordinal)
                && candidate.Magnitude == magnitude)
            {
                found = candidate;
                return true;
            }
        }

        found = default;
        return false;
    }

    private static bool Matches(IReadOnlyList<string>? formulas, string formula) =>
        formulas is null || formulas.Count == 0 || Contains(formulas, formula);

    private static bool Contains(IReadOnlyList<string>? formulas, string formula) =>
        formulas is not null
        && formulas.Any(candidate => string.Equals(candidate, formula, StringComparison.Ordinal));

    private static Solubility Reversed(Solubility solubility) =>
        solubility == Solubility.Soluble ? Solubility.Insoluble : Solubility.Soluble;

    private static bool Holds(IReadOnlyList<string> formulas, string formula) =>
        formulas.Any(candidate => string.Equals(candidate, formula, StringComparison.Ordinal));
}
