namespace ReactionLab.Chemistry.Thermochemistry;

public readonly record struct SpeciesStandardState(
    string Formula,
    Phase Phase,
    decimal FormationEnthalpyKjPerMol,
    decimal? StandardEntropyJPerMolKelvin = null);
