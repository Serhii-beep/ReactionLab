namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record IonDataset(
    IReadOnlyList<IonEntry> Cations,
    IReadOnlyList<IonEntry> Anions,
    IonBehaviorsEntry Behaviors,
    IReadOnlyList<SolubilityRuleEntry> Solubility);
