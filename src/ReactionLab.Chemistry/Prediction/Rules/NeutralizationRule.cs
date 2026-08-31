using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class NeutralizationRule(IonTable table) : IReactionRule
{
    public string Name => "neutralization";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var acid = 0; acid < reactants.Count; acid++)
        {
            if (!table.TryReadAcid(reactants[acid].Formula, out var anion) || table.IsUnstableAcid(anion))
            {
                continue;
            }

            for (var alkali = 0; alkali < reactants.Count; alkali++)
            {
                if (alkali == acid || !table.TryReadBase(reactants[alkali].Formula, out var cation))
                {
                    continue;
                }

                yield return new PredictedReaction(
                    [acid, alkali],
                    [IonicFormula.Combine(cation.Formula, cation.Magnitude, anion.Formula, anion.Magnitude), "H2O"],
                    "neutralization.saltAndWater",
                    0.9m);
            }
        }
    }
}
