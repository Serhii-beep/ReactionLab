namespace ReactionLab.Chemistry.Thermochemistry;

public sealed record PhaseAssignment(IReadOnlyList<Phase> Reactants, IReadOnlyList<Phase> Products);
