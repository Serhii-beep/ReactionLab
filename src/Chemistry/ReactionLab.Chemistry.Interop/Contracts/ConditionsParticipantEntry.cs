using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ConditionsParticipantEntry(
    string Formula,
    Phase Phase,
    int StoichiometricNumber,
    double Moles);
