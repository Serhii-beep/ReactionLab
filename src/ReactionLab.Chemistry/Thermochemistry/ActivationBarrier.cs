namespace ReactionLab.Chemistry.Thermochemistry;

public readonly record struct ActivationBarrier(
    string Family,
    decimal IntrinsicKjPerMol,
    decimal TransferCoefficient,
    decimal MinimumKjPerMol);
