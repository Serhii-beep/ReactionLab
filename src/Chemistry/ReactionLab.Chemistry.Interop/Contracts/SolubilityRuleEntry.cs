using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record SolubilityRuleEntry(
    string Code,
    Solubility Solubility,
    IReadOnlyList<string>? Cations = null,
    IReadOnlyList<string>? Anions = null,
    IReadOnlyList<string>? ExceptCations = null);
