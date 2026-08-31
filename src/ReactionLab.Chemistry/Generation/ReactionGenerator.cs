using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;

namespace ReactionLab.Chemistry.Generation;

public sealed class ReactionGenerator(ReactionPredictor predictor)
{
    public IReadOnlyList<GeneratedReaction> From(IReadOnlyList<Reagent> substances)
    {
        var found = new Dictionary<string, GeneratedReaction>(StringComparer.Ordinal);

        for (var first = 0; first < substances.Count; first++)
        {
            Collect(found, [substances[first]]);

            for (var second = first + 1; second < substances.Count; second++)
            {
                Collect(found, [substances[first], substances[second]]);
            }
        }

        return [.. found.Values
            .OrderByDescending(reaction => reaction.Confidence)
            .ThenBy(reaction => reaction.Signature, StringComparer.Ordinal)];
    }

    private void Collect(Dictionary<string, GeneratedReaction> found, IReadOnlyList<Reagent> bag)
    {
        foreach (var prediction in predictor.Predict(bag))
        {
            if (!TryBalance(prediction, bag, out var generated))
            {
                continue;
            }

            if (!found.TryGetValue(generated.Signature, out var existing)
                || generated.Confidence > existing.Confidence
                || (generated.Confidence == existing.Confidence
                    && string.CompareOrdinal(generated.Rule, existing.Rule) < 0))
            {
                found[generated.Signature] = generated;
            }
        }
    }

    private static bool TryBalance(
        PredictedReaction prediction, IReadOnlyList<Reagent> bag, out GeneratedReaction generated)
    {
        generated = null!;
        var products = new List<Reagent>(prediction.Products.Count);

        foreach (var formula in prediction.Products)
        {
            if (!Reagent.TryCreate(formula, out var product))
            {
                return false;
            }

            products.Add(product);
        }

        var reactants = prediction.Reactants.Select(i => bag[i]).ToList();

        if (!EquationBalancer.TryBalance(
            [.. reactants.Select(reactant => reactant.Composition)],
            [.. products.Select(product => product.Composition)],
            out var balanced,
            out _))
        {
            return false;
        }

        generated = new GeneratedReaction(
            [.. reactants.Select((reactant, index) =>
                new GeneratedParticipant(reactant.Formula, balanced.ReactantCoefficients[index]))],
            [.. products.Select((product, index) =>
                new GeneratedParticipant(product.Formula, balanced.ProductCoefficients[index]))],
            prediction.Rule,
            prediction.Confidence,
            prediction.MinimumKelvin);

        return true;
    }
}
