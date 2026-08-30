using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class SingleReplacementRule(ActivitySeries series, IonTable table) : IReactionRule
{
    private const decimal Steam = 373.15m;

    public string Name => "singleReplacement";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var i = 0; i < reactants.Count; i++)
        {
            if (!IsBareMetal(reactants[i].Composition, out var symbol)
                || !series.TryFind(symbol, out var metal))
            {
                continue;
            }

            for (var other = 0; other < reactants.Count; other++)
            {
                if (other == i)
                {
                    continue;
                }

                foreach (var prediction in Against(i, metal, other, reactants[other]))
                {
                    yield return prediction;
                }
            }
        }
    }

    private PredictedReaction[] Against(
        int metalIndex, ActiveMetal metal, int otherIndex, Reagent other)
    {
        if (string.Equals(other.Formula, "H2O", StringComparison.Ordinal))
        {
            return Water(metalIndex, metal, otherIndex);
        }

        if (IonicFormula.TryReadAcid(other.Formula, out var anion, out var anionCharge))
        {
            return series.Displaces(metal.Symbol, "H")
                ? [Displacement(metalIndex, metal, otherIndex, anion, anionCharge, "H2", "metalAndAcid", 0.9m)]
                : [];
        }

        return table.TrySplit(other.Formula, out var cation, out var saltAnion)
            && series.Displaces(metal.Symbol, cation.Formula)
                ? [Displacement(
                    metalIndex, metal, otherIndex, saltAnion.Formula, saltAnion.Magnitude, cation.Formula, "metalAndSalt", 0.85m)]
                : [];
    }

    private static PredictedReaction[] Water(int metalIndex, ActiveMetal metal, int waterIndex) =>
        metal.Water switch
        {
            WaterReactivity.Cold =>
            [
                new PredictedReaction(
                    [metalIndex, waterIndex],
                    [IonicFormula.Combine(metal.Symbol, metal.Charge, "OH", 1), "H2"],
                    "singleReplacement.metalAndColdWater",
                    0.9m)
            ],
            WaterReactivity.Steam =>
            [
                new PredictedReaction(
                    [metalIndex, waterIndex],
                    [IonicFormula.Combine(metal.Symbol, metal.Charge, "O", 2), "H2"],
                    "singleReplacement.metalAndSteam",
                    0.8m,
                    Steam)
            ],
            _ => []
        };

    private static PredictedReaction Displacement(
        int metalIndex,
        ActiveMetal metal,
        int otherIndex,
        string anion,
        int anionCharge,
        string displaced,
        string rule,
        decimal confidence) =>
        new(
            [metalIndex, otherIndex],
            [IonicFormula.Combine(metal.Symbol, metal.Charge, anion, anionCharge), displaced],
            $"singleReplacement.{rule}",
            confidence);

    private static bool IsBareMetal(ChemicalComposition composition, out string symbol)
    {
        if (composition is { Charge: 0, Elements: [{ Symbol: var only, Count: 1 }] })
        {
            symbol = only;
            return true;
        }

        symbol = string.Empty;
        return false;
    }
}
