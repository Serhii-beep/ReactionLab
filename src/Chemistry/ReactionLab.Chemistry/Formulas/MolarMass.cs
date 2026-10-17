namespace ReactionLab.Chemistry.Formulas;

public static class MolarMass
{
    public static bool TryCompute(
        ChemicalComposition composition,
        IReadOnlyDictionary<string, decimal> atomicMasses,
        out decimal gramsPerMole,
        out string? unknownSymbol)
    {
        gramsPerMole = 0m;
        unknownSymbol = null;

        foreach (var (symbol, count) in composition.Elements)
        {
            if (!atomicMasses.TryGetValue(symbol, out var mass))
            {
                unknownSymbol = symbol;
                gramsPerMole = 0m;
                return false;
            }

            gramsPerMole += mass * count;
        }

        return true;
    }

    public static IReadOnlyList<(string Symbol, decimal Percent)> PercentComposition(
        ChemicalComposition composition,
        IReadOnlyDictionary<string, decimal> atomicMasses)
    {
        if (!TryCompute(composition, atomicMasses, out var total, out _) || total == 0m)
        {
            return [];
        }

        return
        [
            .. composition.Elements.Select(element =>
            (element.Symbol, atomicMasses[element.Symbol] * element.Count / total * 100))
        ];
    }
}
