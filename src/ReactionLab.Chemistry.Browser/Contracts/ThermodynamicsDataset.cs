namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ThermodynamicsDataset(
    IReadOnlyList<SpeciesEnthalpyEntry> Species,
    IReadOnlyList<AqueousIonEntry> AqueousIons,
    IReadOnlyList<ActivationBarrierEntry> Barriers);
