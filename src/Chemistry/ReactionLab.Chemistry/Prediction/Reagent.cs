using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.Chemistry.Prediction;

public readonly record struct Reagent(string Formula, ChemicalComposition Composition)
{
    public static bool TryCreate(string? formula, out Reagent reagent)
    {
        if (!FormulaParser.TryParse(formula, out var composition, out _))
        {
            reagent = default;
            return false;
        }

        return (reagent = new Reagent(formula!.Trim(), composition)) is var _;
    }
}
