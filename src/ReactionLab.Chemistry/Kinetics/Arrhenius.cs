using ReactionLab.Chemistry.Quantities;

namespace ReactionLab.Chemistry.Kinetics;

public static class Arrhenius
{
    public static double Log10RateFactor(decimal activationEnergyKjPerMol, decimal kelvin, decimal referenceKelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(activationEnergyKjPerMol);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceKelvin);

        return -(double)activationEnergyKjPerMol * 1000d / (double)IdealGas.GasConstant
            * ((1d / (double)kelvin) - (1d / (double)referenceKelvin)) / Math.Log(10d);
    }

    public static double Log10ShareOverBarrier(decimal activationEnergyKjPerMol, decimal kelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(activationEnergyKjPerMol);

        return -(double)activationEnergyKjPerMol / (double)ThermalEnergyKjPerMol(kelvin) / Math.Log(10d);
    }

    public static decimal ThermalEnergyKjPerMol(decimal kelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);

        return IdealGas.GasConstant * kelvin / 1000m;
    }
}
