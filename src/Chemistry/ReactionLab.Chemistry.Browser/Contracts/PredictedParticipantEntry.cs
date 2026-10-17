using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record PredictedParticipantEntry(string Formula, int Coefficient, Phase? Phase);
