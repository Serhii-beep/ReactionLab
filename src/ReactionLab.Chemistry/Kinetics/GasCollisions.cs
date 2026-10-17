namespace ReactionLab.Chemistry.Kinetics;

public static class GasCollisions
{
    public static double Log10FrequencyFactor(decimal kelvin, decimal kilopascals, decimal referenceKelvin, decimal referenceKilopascals)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilopascals);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceKelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceKilopascals);

        return Math.Log10((double)(kilopascals / referenceKilopascals)) + (0.5d * Math.Log10((double)(referenceKelvin / kelvin)));
    }
}
