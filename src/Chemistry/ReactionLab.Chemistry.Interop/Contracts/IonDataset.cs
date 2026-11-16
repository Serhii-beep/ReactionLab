namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record IonDataset(
    IReadOnlyList<IonEntry> Cations,
    IReadOnlyList<IonEntry> Anions,
    IonBehaviorsEntry Behaviors,
    IReadOnlyList<SolubilityRuleEntry> Solubility);
