using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class PrecipitationRule(IonTable table) : IReactionRule
{
    public string Name => "precipitation";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants)
    {
        for (var first = 0; first < reactants.Count; first++)
        {
            if (!Dissolved(reactants[first].Formula, out var firstCation, out var firstAnion))
            {
                continue;
            }

            for (var second = first + 1; second < reactants.Count; second++)
            {
                if (!Dissolved(reactants[second].Formula, out var secondCation, out var secondAnion))
                {
                    continue;
                }

                var swapped = IonicFormula.Combine(
                    firstCation.Formula, firstCation.Magnitude,
                    secondAnion.Formula, secondAnion.Magnitude);

                var partner = IonicFormula.Combine(
                    secondCation.Formula, secondCation.Magnitude,
                    firstAnion.Formula, firstAnion.Magnitude);

                var precipitate = Precipitate(firstCation, secondAnion, secondCation, firstAnion);

                if (precipitate is not null)
                {
                    yield return new PredictedReaction(
                        [first, second], [swapped, partner], $"precipitation.{precipitate}", 0.85m);
                }
            }
        }
    }

    private string? Precipitate(Ion firstCation, Ion secondAnion, Ion secondCation, Ion firstAnion)
    {
        if (table.SolubilityOf(firstCation, secondAnion, out var swappedRule) == Solubility.Insoluble)
        {
            return swappedRule;
        }

        return table.SolubilityOf(secondCation, firstAnion, out var partnerRule) == Solubility.Insoluble
            ? partnerRule
            : null;
    }

    private bool Dissolved(string formula, out Ion cation, out Ion anion) =>
        table.TrySplit(formula, out cation, out anion)
        && table.SolubilityOf(cation, anion, out _) == Solubility.Soluble;
}
