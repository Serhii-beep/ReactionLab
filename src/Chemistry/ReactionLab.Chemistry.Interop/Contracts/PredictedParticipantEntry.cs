using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record PredictedParticipantEntry(string Formula, int Coefficient, Phase? Phase);
