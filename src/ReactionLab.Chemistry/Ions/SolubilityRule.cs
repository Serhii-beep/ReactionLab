namespace ReactionLab.Chemistry.Ions;

public sealed record SolubilityRule(
    string Code,
    Solubility Solubility,
    IReadOnlyList<string>? Cations = null,
    IReadOnlyList<string>? Anions = null,
    IReadOnlyList<string>? ExceptCations = null);
