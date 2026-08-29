using System.Globalization;

namespace ReactionLab.Chemistry.Quantities;

public readonly record struct Volume
{
    private Volume(decimal liters) => Liters = liters;

    public decimal Liters { get; }

    public static Volume FromLiters(decimal liters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(liters);

        return new Volume(liters);
    }

    public static Volume FromMilliliters(decimal milliliters) => FromLiters(milliliters / 1000m);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Liters} L");
}
