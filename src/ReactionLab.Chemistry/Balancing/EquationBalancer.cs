using System.Numerics;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Numerics;

namespace ReactionLab.Chemistry.Balancing;

public static class EquationBalancer
{
    public static ChemicalComposition Electron { get; } = new(new Dictionary<string, int>(StringComparer.Ordinal), -1);

    public static bool TryBalance(
        IReadOnlyList<ChemicalComposition> reactants,
        IReadOnlyList<ChemicalComposition> products,
        out BalancedEquation balanced,
        out BalanceError error)
    {
        balanced = null!;

        if (reactants.Count == 0 || products.Count == 0)
        {
            error = BalanceError.EmptySide;
            return false;
        }

        List<ChemicalComposition> species = [.. reactants, .. products];
        var matrix = BuildMatrix(species, reactants.Count);
        var pivots = ReduceToRowEchelonForm(matrix, species.Count);

        var nullity = species.Count - pivots.Count;

        if (nullity != 1)
        {
            error = nullity == 0 ? BalanceError.Unbalanceable : BalanceError.UnderDetermined;
            return false;
        }

        if (!TryWholeNumbers(NullSpaceVector(matrix, pivots, species.Count), out var coefficients))
        {
            error = BalanceError.CoefficientOverflow;
            return false;
        }

        if (Array.Exists(coefficients, coefficient => coefficient <= 0))
        {
            error = BalanceError.Unbalanceable;
            return false;
        }

        balanced = new BalancedEquation(coefficients[..reactants.Count], coefficients[reactants.Count..]);
        error = BalanceError.None;
        return true;
    }

    private static Rational[][] BuildMatrix(List<ChemicalComposition> species, int reactantCount)
    {
        var symbols = species
            .SelectMany(composition => composition.Elements.Select(element => element.Symbol))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var matrix = new Rational[symbols.Count + 1][];

        for (var row = 0; row < symbols.Count; row++)
        {
            matrix[row] = new Rational[species.Count];

            for (var column = 0; column < species.Count; column++)
            {
                matrix[row][column] = Signed(species[column].CountOf(symbols[row]), column, reactantCount);
            }
        }

        matrix[symbols.Count] = new Rational[species.Count];

        for (var column = 0; column < species.Count; column++)
        {
            matrix[symbols.Count][column] = Signed(species[column].Charge, column, reactantCount);
        }

        return matrix;
    }

    private static Rational Signed(int value, int column, int reactantCount) =>
        Rational.From(column < reactantCount ? value : -value);

    private static List<int> ReduceToRowEchelonForm(Rational[][] matrix, int columns)
    {
        var pivots = new List<int>();
        var row = 0;

        for (var column = 0; column < columns && row < matrix.Length; column++)
        {
            var pivot = FindPivot(matrix, row, column);

            if (pivot < 0)
            {
                continue;
            }

            (matrix[row], matrix[pivot]) = (matrix[pivot], matrix[row]);

            var head = matrix[row][column];

            for (var i = column; i < columns; i++)
            {
                matrix[row][i] /= head;
            }

            EliminateColumn(matrix, row, column, columns);
            pivots.Add(column);
            row++;
        }

        return pivots;
    }

    private static int FindPivot(Rational[][] matrix, int fromRow, int column)
    {
        for (var candidate = fromRow; candidate < matrix.Length; candidate++)
        {
            if (!matrix[candidate][column].IsZero)
            {
                return candidate;
            }
        }

        return -1;
    }

    private static void EliminateColumn(Rational[][] matrix, int pivotRow, int column, int columns)
    {
        for (var row = 0; row < matrix.Length; row++)
        {
            if (row == pivotRow || matrix[row][column].IsZero)
            {
                continue;
            }

            var factor = matrix[row][column];

            for (var i = column; i < columns; i++)
            {
                matrix[row][i] -= factor * matrix[pivotRow][i];
            }
        }
    }

    private static Rational[] NullSpaceVector(Rational[][] matrix, List<int> pivots, int columns)
    {
        var free = 0;

        while (pivots.Contains(free))
        {
            free++;
        }

        var vector = new Rational[columns];
        vector[free] = Rational.One;

        for (var row = 0; row < pivots.Count; row++)
        {
            vector[pivots[row]] = -matrix[row][free];
        }

        return vector;
    }

    private static bool TryWholeNumbers(Rational[] solution, out int[] coefficients)
    {
        var multiplier = BigInteger.One;

        foreach (var value in solution)
        {
            multiplier = multiplier / BigInteger.GreatestCommonDivisor(multiplier, value.Denominator) * value.Denominator;
        }

        var scaled = Array.ConvertAll(solution, value => value.Numerator * (multiplier / value.Denominator));
        var divisor = scaled.Aggregate(
            BigInteger.Zero,
            (acc, value) => BigInteger.GreatestCommonDivisor(acc, BigInteger.Abs(value)));

        coefficients = new int[scaled.Length];

        for (var i = 0; i < scaled.Length; i++)
        {
            var whole = scaled[i] / divisor;

            if (whole > int.MaxValue || whole < int.MinValue)
            {
                return false;
            }

            coefficients[i] = (int)whole;
        }

        return true;
    }
}
