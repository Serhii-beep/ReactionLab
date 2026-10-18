using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Chemistry.Constitution;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Catalog.Infrastructure.Persistence;

internal static class StructureKeys
{
    public static string? Of(MolecularStructure? structure) =>
        structure is null
            ? null
            : ConstitutionKey.Of(
                [.. structure.Atoms.Select(atom => atom.Symbol.Value)],
                [.. structure.Bonds.Select(bond => new AtomBond(bond.FromAtomIndex, bond.ToAtomIndex, 1))]);
}
