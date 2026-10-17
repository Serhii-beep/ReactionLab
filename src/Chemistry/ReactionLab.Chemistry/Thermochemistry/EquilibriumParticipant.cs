namespace ReactionLab.Chemistry.Thermochemistry;

public readonly record struct EquilibriumParticipant(
    string Formula,
    Phase Phase,
    int StoichiometricNumber,
    double InitialMoles);
