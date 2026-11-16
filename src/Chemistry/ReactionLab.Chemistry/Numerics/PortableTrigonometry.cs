namespace ReactionLab.Chemistry.Numerics;

internal static class PortableTrigonometry
{
    private const double HalfPi = Math.PI / 2d;
    private const double HalfPiHead = 1.57079632673412561417;
    private const double HalfPiTail = 6.07710050650619224932e-11;

    private static readonly double[] SineCoefficients =
        [1d, -1d / 6, 1d / 120, -1d / 5040, 1d / 362880, -1d / 39916800, 1d / 6227020800, -1d / 1307674368000];

    private static readonly double[] CosineCoefficients =
    [
        1d, -1d / 2, 1d / 24, -1d / 720, 1d / 40320, -1d / 3628800, 1d / 479001600, -1d / 87178291200,
        1d / 20922789888000
    ];

    private static readonly double[] ArctangentCoefficients =
    [
        1d, -1d / 3, 1d / 5, -1d / 7, 1d / 9, -1d / 11, 1d / 13, -1d / 15, 1d / 17, -1d / 19, 1d / 21, -1d / 23,
        1d / 25
    ];

    public static double Cos(double radians) => SinCos(radians).Cosine;

    public static (double Sine, double Cosine) SinCos(double radians)
    {
        var quarterTurns = Math.Round(radians / HalfPi);
        var remainingRadians = radians - (quarterTurns * HalfPiHead) - (quarterTurns * HalfPiTail);
        var remainingSquared = remainingRadians * remainingRadians;
        var sine = remainingRadians * PolynomialAt(SineCoefficients, remainingSquared);
        var cosine = PolynomialAt(CosineCoefficients, remainingSquared);

        return ((long)quarterTurns & 3) switch
        {
            0 => (sine, cosine),
            1 => (cosine, -sine),
            2 => (-sine, -cosine),
            _ => (-cosine, sine)
        };
    }

    public static double Atan2(double opposite, double adjacent)
    {
        if (Math.Abs(opposite) > Math.Abs(adjacent))
        {
            return (opposite > 0d ? HalfPi : -HalfPi) - Atan(adjacent / opposite);
        }

        if (adjacent == 0d)
        {
            return 0d;
        }

        var angle = Atan(opposite / adjacent);

        return adjacent > 0d ? angle : angle + (opposite < 0d ? -Math.PI : Math.PI);
    }

    public static double Acos(double cosine) => Atan2(Math.Sqrt((1d - cosine) * (1d + cosine)), cosine);

    private static double Atan(double tangent)
    {
        var halfAngleTangent = tangent / (1d + Math.Sqrt(1d + (tangent * tangent)));
        var quarterAngleTangent = halfAngleTangent / (1d + Math.Sqrt(1d + (halfAngleTangent * halfAngleTangent)));
        var quarterAngleTangentSquared = quarterAngleTangent * quarterAngleTangent;

        return 4d * quarterAngleTangent * PolynomialAt(ArctangentCoefficients, quarterAngleTangentSquared);
    }

    private static double PolynomialAt(double[] coefficients, double squaredArgument)
    {
        var sum = coefficients[^1];

        for (var index = coefficients.Length - 2; index >= 0; index--)
        {
            sum = (sum * squaredArgument) + coefficients[index];
        }

        return sum;
    }
}
