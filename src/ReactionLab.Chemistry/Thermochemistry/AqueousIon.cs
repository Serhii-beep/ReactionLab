using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Thermochemistry;

public readonly record struct AqueousIon(Ion Ion, decimal FormationEnthalpyKjPerMol);
