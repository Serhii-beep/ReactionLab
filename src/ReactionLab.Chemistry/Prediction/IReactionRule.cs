using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.Chemistry.Prediction;

public interface IReactionRule
{
    string Name { get; }

    IEnumerable<PredictedReaction> Predict(IReadOnlyList<ChemicalComposition> reactants);
}
