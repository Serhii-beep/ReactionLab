using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class DecompositionRule(IonTable table) : IReactionRule
{
    public string Name => "decomposition";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var i = 0; i < reactants.Count; i++)
        {
            if (!table.TrySplit(reactants[i].Formula, out var cation, out var anion))
            {
                continue;
            }

            var oxide = IonicFormula.Combine(cation.Formula, cation.Magnitude, "O", 2);

            switch (anion.Formula)
            {
                case "CO3":
                    yield return new PredictedReaction(
                        [i], [oxide, "CO2"], "decomposition.carbonate", 0.85m);
                    break;
                case "HCO3":
                    yield return new PredictedReaction(
                        [i],
                        [
                            IonicFormula.Combine(cation.Formula, cation.Magnitude, "CO3", 2),
                            "H2O",
                            "CO2"
                        ],
                        "decomposition.hydrogencarbonate",
                        0.85m);
                    break;
                case "OH":
                    yield return new PredictedReaction(
                        [i], [oxide, "H2O"], "decomposition.hydroxide", 0.8m);
                    break;
                default:
                    break;
            }
        }
    }
}
