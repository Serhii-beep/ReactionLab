using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.Chemistry.Prediction.Rules;

public sealed class CombustionRule : IReactionRule
{
    private const string Carbon = "C";
    private const string Hydrogen = "H";
    private const string Oxygen = "O";

    public string Name => "combustion";

    public IEnumerable<PredictedReaction> Predict(IReadOnlyList<ChemicalComposition> reactants)
    {
        var oxygen = IndexOfMolecularOxygen(reactants);

        if (oxygen < 0)
        {
            yield break;
        }

        for (var i = 0; i < reactants.Count; i++)
        {
            var fuel = reactants[i];

            if (i == oxygen || !IsFuel(fuel))
            {
                continue;
            }

            int[] used = [i, oxygen];
            var water = fuel.CountOf(Hydrogen) > 0;

            if (ConsumesOxygen(fuel, 2))
            {
                yield return new PredictedReaction(used, Burnt("CO2", water), "combustion.complete", 0.95m);
            }

            if (ConsumesOxygen(fuel, 1))
            {
                yield return new PredictedReaction(used, Burnt("CO", water), "combustion.incomplete", 0.5m);
            }

            if (ConsumesOxygen(fuel, 0))
            {
                yield return new PredictedReaction(used, Burnt("C", water), "combustion.sooting", 0.3m);
            }
        }
    }

    private static string[] Burnt(string carbonProduct, bool water) =>
        water ? [carbonProduct, "H2O"] : [carbonProduct];

    private static bool ConsumesOxygen(ChemicalComposition fuel, int oxygenPerCarbon) =>
        2 * fuel.CountOf(Oxygen) < 2 * oxygenPerCarbon * fuel.CountOf(Carbon) + fuel.CountOf(Hydrogen);

    private static int IndexOfMolecularOxygen(IReadOnlyList<ChemicalComposition> reactants)
    {
        for (var i = 0; i < reactants.Count; i++)
        {
            if (reactants[i] is { Charge: 0, Elements: [{ Symbol: Oxygen, Count: 2 }] })
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsFuel(ChemicalComposition composition)
    {
        if (composition.Charge != 0 || composition.CountOf(Carbon) == 0)
        {
            return false;
        }

        foreach (var (symbol, _) in composition.Elements)
        {
            if (symbol is not (Carbon or Hydrogen or Oxygen))
            {
                return false;
            }
        }

        return true;
    }
}
