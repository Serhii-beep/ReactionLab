using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.Chemistry.Prediction;

public sealed class ReactionPredictor(IEnumerable<IReactionRule> rules)
{
    private readonly IReadOnlyList<IReactionRule> _rules = [.. rules];

    public IReadOnlyList<PredictedReaction> Predict(IReadOnlyList<ChemicalComposition> reactants) =>
        reactants.Count == 0
            ? []
            : [.. _rules
                    .SelectMany(rule => rule.Predict(reactants))
                    .OrderByDescending(prediction => prediction.Confidence)
                    .ThenBy(prediction => prediction.Rule, StringComparer.Ordinal)];
}
