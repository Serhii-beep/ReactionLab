namespace ReactionLab.Chemistry.Geometry;

public readonly record struct DihedralAngle(double Cosine, double Sine)
{
    public static readonly DihedralAngle Eclipsed = new(1d, 0d);

    public static readonly DihedralAngle Perpendicular = new(0d, 1d);

    public static readonly DihedralAngle Anti = new(-1d, 0d);

    public static DihedralAngle Of(double opposite, double adjacent)
    {
        var length = Math.Sqrt((opposite * opposite) + (adjacent * adjacent));

        return new DihedralAngle(adjacent / length, opposite / length);
    }

    public DihedralAngle Minus(DihedralAngle other) =>
        new((Cosine * other.Cosine) + (Sine * other.Sine), (Sine * other.Cosine) - (Cosine * other.Sine));

    public DihedralAngle Times(int multiple)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiple);

        var product = this;

        for (var turn = 1; turn < multiple; turn++)
        {
            product = new DihedralAngle(
                (product.Cosine * Cosine) - (product.Sine * Sine),
                (product.Sine * Cosine) + (product.Cosine * Sine));
        }

        return product;
    }
}
