using System.Globalization;

namespace ReactionLab.Chemistry.Quantities;

public readonly record struct Mass
{
    private Mass(decimal grams) => Grams = grams;

    public decimal Grams { get; }

    public static Mass FromGrams(decimal grams)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grams);

        return new Mass(grams);
    }

    public Amount ToAmount(decimal gramsPerMole)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gramsPerMole);

        return Amount.FromMoles(Grams / gramsPerMole);
    }

    public decimal AsPercentageOf(Mass whole)
    {
        ArgumentOutOfRangeException.ThrowIfZero(whole.Grams, nameof(whole));

        return Grams / whole.Grams * 100m;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Grams} g");
}
