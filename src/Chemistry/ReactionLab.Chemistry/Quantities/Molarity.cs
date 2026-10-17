using System.Globalization;

namespace ReactionLab.Chemistry.Quantities;

public readonly record struct Molarity
{
    private Molarity(decimal molesPerLiter) => MolesPerLiter = molesPerLiter;

    public decimal MolesPerLiter { get; }

    public static Molarity FromMolesPerLiter(decimal molesPerLiter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(molesPerLiter);

        return new Molarity(molesPerLiter);
    }

    public static Molarity Of(Amount solute, Volume solution)
    {
        ArgumentOutOfRangeException.ThrowIfZero(solution.Liters, nameof(solution));

        return FromMolesPerLiter(solute.Moles / solution.Liters);
    }

    public Amount In(Volume volume) => Amount.FromMoles(MolesPerLiter * volume.Liters);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{MolesPerLiter} M");
}
