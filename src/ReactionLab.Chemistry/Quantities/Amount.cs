using System.Globalization;

namespace ReactionLab.Chemistry.Quantities;

public readonly record struct Amount
{
    private Amount(decimal moles) => Moles = moles;

    public decimal Moles { get; }

    public static Amount FromMoles(decimal moles)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(moles);

        return new Amount(moles);
    }

    public Mass ToMass(decimal gramsPerMole)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gramsPerMole);

        return Mass.FromGrams(Moles * gramsPerMole);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Moles} mol");
}
