namespace ReactionLab.Catalog.Application.Features.Substances.Contracts;

public sealed record MolecularStructureResponse(
    IReadOnlyList<AtomResponse> Atoms,
    IReadOnlyList<BondResponse> Bonds);
