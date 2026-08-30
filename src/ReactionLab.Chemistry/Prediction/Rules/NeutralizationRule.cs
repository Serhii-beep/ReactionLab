using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class NeutralizationRule : IReactionRule
{
    public string Name => "neutralization";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var acid = 0; acid < reactants.Count; acid++)
        {
            if (!IonicFormula.TryReadAcid(reactants[acid].Formula, out var anion, out var anionCharge))
            {
                continue;
            }

            for (var alkali = 0; alkali < reactants.Count; alkali++)
            {
                if (alkali == acid || !IonicFormula.TryReadBase(reactants[alkali].Formula, out var cation, out var cationCharge))
                {
                    continue;
                }

                yield return new PredictedReaction(
                    [acid, alkali],
                    [IonicFormula.Combine(cation, cationCharge, anion, anionCharge), "H2O"],
                    "neutralization.saltAndWater",
                    0.9m);
            }
        }
    }
}
