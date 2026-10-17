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
