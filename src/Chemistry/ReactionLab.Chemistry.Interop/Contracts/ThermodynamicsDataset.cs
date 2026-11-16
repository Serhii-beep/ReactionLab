namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ThermodynamicsDataset(
    IReadOnlyList<SpeciesStandardStateEntry> Species,
    IReadOnlyList<AqueousIonEntry> AqueousIons,
    IReadOnlyList<ActivationBarrierEntry> Barriers);
