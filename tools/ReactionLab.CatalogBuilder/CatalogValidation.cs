using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.CatalogBuilder;

internal sealed class CatalogValidation(IReadOnlySet<string> knownFormulas)
{
    public bool Accepts(Candidate candidate, out string rejection)
    {
        rejection = string.Empty;

        var missing = candidate.All
            .Select(participant => participant.Formula)
            .Where(formula => !knownFormulas.Contains(formula))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (missing.Count > 0)
        {
            rejection = $"no substance is seeded for {string.Join(", ", missing)}";

            return false;
        }

        if (candidate.All.Any(participant => participant.Phase is null))
        {
            rejection = "at least one participant has no phase";

            return false;
        }

        if (!Conserves(candidate, out var detail))
        {
            rejection = detail;

            return false;
        }

        return true;
    }

    private static bool Conserves(Candidate candidate, out string detail)
    {
        detail = string.Empty;

        if (!Tally(candidate.Reactants, out var left, out var leftCharge)
            || !Tally(candidate.Products, out var right, out var rightCharge))
        {
            detail = "a formula could not be parsed";

            return false;
        }

        if (leftCharge != rightCharge)
        {
            detail = $"charge is not conserved: {leftCharge} against {rightCharge}";

            return false;
        }

        foreach (var symbol in left.Keys.Union(right.Keys, StringComparer.Ordinal))
        {
            left.TryGetValue(symbol, out var before);
            right.TryGetValue(symbol, out var after);

            if (before != after)
            {
                detail = $"mass is not conserved: {symbol} {before} against {after}";

                return false;
            }
        }

        return true;
    }

    private static bool Tally(
        IEnumerable<CandidateParticipant> side, out Dictionary<string, int> tally, out int charge)
    {
        tally = new Dictionary<string, int>(StringComparer.Ordinal);
        charge = 0;

        foreach (var participant in side)
        {
            if (!FormulaParser.TryParse(participant.Formula, out var composition, out _))
            {
                return false;
            }

            charge += composition.Charge * participant.Coefficient;

            foreach (var (symbol, count) in composition.Elements)
            {
                tally.TryGetValue(symbol, out var running);
                tally[symbol] = running + count * participant.Coefficient;
            }
        }

        return true;
    }
}
