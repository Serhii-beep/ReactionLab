namespace ReactionLab.Chemistry.Quantities;

public static class IdealGas
{
    public const decimal GasConstant = 8.314462618m;

    public static Volume VolumeOf(Amount amount, decimal kelvin, decimal kilopascals)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilopascals);

        return Volume.FromLiters(amount.Moles * GasConstant * kelvin / kilopascals);
    }

    public static Amount AmountIn(Volume volume, decimal kelvin, decimal kilopascals)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilopascals);

        return Amount.FromMoles(kilopascals * volume.Liters / (GasConstant * kelvin));
    }
}
