namespace ReactionLab.Chemistry.Thermochemistry;

public readonly record struct StandardState(
    decimal FormationEnthalpyKjPerMol,
    decimal? StandardEntropyJPerMolKelvin);
