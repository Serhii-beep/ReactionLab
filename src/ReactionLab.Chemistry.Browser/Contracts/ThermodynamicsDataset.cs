namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ThermodynamicsDataset(
    IReadOnlyList<SpeciesStandardStateEntry> Species,
    IReadOnlyList<AqueousIonEntry> AqueousIons,
    IReadOnlyList<ActivationBarrierEntry> Barriers);
