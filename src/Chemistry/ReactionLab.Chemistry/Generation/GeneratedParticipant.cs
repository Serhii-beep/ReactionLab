using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Generation;

public readonly record struct GeneratedParticipant(string Formula, int Coefficient, Phase? Phase);
