using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class SynthesisRule(ActivitySeries series, IonTable table) : IReactionRule
{
    public string Name => "synthesis";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var first = 0; first < reactants.Count; first++)
        {
            for (var second = 0; second < reactants.Count; second++)
            {
                if (first == second)
                {
                    continue;
                }

                foreach (var prediction in Combining(reactants, first, second))
                {
                    yield return prediction;
                }
            }
        }
    }

    private IEnumerable<PredictedReaction> Combining(
        IReadOnlyList<Reagent> reactants, int first, int second)
    {
        if (Uncombined(reactants[first], out var metal)
            && Uncombined(reactants[second], out var nonMetal)
            && series.TryFind(metal, out var active)
            && Anion(nonMetal) is { } anion
            && !DeclinedWithHydrogen(active, anion))
        {
            return
            [
                new PredictedReaction(
                    [first, second],
                    [IonicFormula.Combine(active.Symbol, active.Charge, anion.Formula, anion.Magnitude)],
                    "synthesis.metalAndNonMetal",
                    0.9m)
            ];
        }

        if (string.Equals(reactants[second].Formula, "H2O", StringComparison.Ordinal)
            && table.TrySplit(reactants[first].Formula, out var cation, out var oxide)
            && string.Equals(oxide.Formula, "O", StringComparison.Ordinal)
            && series.Displaces(cation.Formula, "Al"))
        {
            return
            [
                new PredictedReaction(
                    [first, second],
                    [IonicFormula.Combine(cation.Formula, cation.Magnitude, "OH", 1)],
                    "synthesis.oxideAndWater",
                    0.85m)
            ];
        }

        return [];
    }

    private static bool DeclinedWithHydrogen(ActiveMetal metal, Ion anion) =>
        string.Equals(metal.Symbol, "H", StringComparison.Ordinal) && anion.Magnitude > 2;

    private Ion? Anion(string symbol)
    {
        foreach (var candidate in table.Anions)
        {
            if (string.Equals(candidate.Formula, symbol, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    internal static bool Uncombined(Reagent reagent, out string symbol)
    {
        if (reagent.Composition is { Charge: 0, Elements: [{ Symbol: var only }] })
        {
            symbol = only;
            return true;
        }

        symbol = string.Empty;
        return false;
    }
}
