using System.Text.Json;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal sealed record MolecularStructureDocument
{
    public IReadOnlyList<AtomDocument> Atoms { get; init; } = [];

    public IReadOnlyList<BondDocument> Bonds { get; init; } = [];

    public static string Serialize(MolecularStructure structure)
    {
        var document = new MolecularStructureDocument
        {
            Atoms = structure.Atoms
                .Select(atom => new AtomDocument(atom.Symbol.Value, atom.X, atom.Y, atom.Z))
                .ToList(),
            Bonds = structure.Bonds
                .Select(bond => new BondDocument(bond.FromAtomIndex, bond.ToAtomIndex, bond.Type))
                .ToList()
        };

        return JsonSerializer.Serialize(document, PersistenceJson.Options);
    }

    public static MolecularStructure Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<MolecularStructureDocument>(json, PersistenceJson.Options)
            ?? throw new InvalidOperationException("Stored structure was not a JSON document.");

        var atoms = document.Atoms.Select(atom => new AtomPlacement(
            PersistenceJson.Require(ElementSymbol.Create(atom.Symbol), "atom symbol"),
            atom.X, atom.Y, atom.Z));

        var bonds = document.Bonds.Select(bond => new Bond(bond.From, bond.To, bond.Type));

        return PersistenceJson.Require(MolecularStructure.Create(atoms, bonds), "structure");
    }
}
