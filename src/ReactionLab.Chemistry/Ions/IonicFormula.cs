using System.Globalization;
using ReactionLab.Chemistry.Formulas;

namespace ReactionLab.Chemistry.Ions;

public sealed class IonicFormula
{
    public static string Combine(string cation, int cationCharge, string anion, int anionCharge)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cationCharge);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(anionCharge);

        var shared = GreatestCommonDivisor(cationCharge, anionCharge);

        return Group(cation, anionCharge / shared) + Group(anion, cationCharge / shared);
    }

    public static bool TryReadAcid(string formula, out string anion, out int charge)
    {
        anion = string.Empty;
        charge = 0;

        if (formula.Length < 2 || formula[0] != 'H' || char.IsAsciiLetterLower(formula[1]))
        {
            return false;
        }

        var index = 1;

        while (index < formula.Length && char.IsAsciiDigit(formula[index]))
        {
            index++;
        }

        charge = index == 1 ? 1 : int.Parse(formula[1..index], CultureInfo.InvariantCulture);
        anion = formula[index..];

        return anion.Length > 0
            && FormulaParser.TryParse(anion, out var composition, out _)
            && composition.Elements.Any(element => element.Symbol != "O");
    }

    public static bool TryReadBase(string formula, out string cation, out int charge)
    {
        cation = string.Empty;
        charge = 0;

        if (formula.EndsWith("OH", StringComparison.Ordinal))
        {
            cation = formula[..^2];
            charge = 1;
        }
        else
        {
            var group = formula.LastIndexOf("(OH)", StringComparison.Ordinal);

            if (group < 1)
            {
                return false;
            }

            var count = formula[(group + 4)..];

            if (count.Length == 0 || !count.All(char.IsAsciiDigit))
            {
                return false;
            }

            cation = formula[..group];
            charge = int.Parse(count, CultureInfo.InvariantCulture);
        }

        return cation.Length > 0 && FormulaParser.TryParse(cation, out _, out _);
    }

    private static string Group(string ion, int count)
    {
        if (count == 1)
        {
            return ion;
        }

        var polyatomic = !FormulaParser.TryParse(ion, out var composition, out _)
            || composition.TotalAtoms > 1;

        return polyatomic
            ? string.Create(CultureInfo.InvariantCulture, $"({ion}){count}")
            : string.Create(CultureInfo.InvariantCulture, $"{ion}{count}");
    }

    private static int GreatestCommonDivisor(int left, int right) =>
        right == 0 ? left : GreatestCommonDivisor(right, left % right);
}
