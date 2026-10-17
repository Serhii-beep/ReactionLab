using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Infrastructure.UnitTests.Documents;

public sealed class MolecularStructureDocumentTests
{
    [Fact]
    public void RoundTrip_PreservesAtomsAndBonds()
    {
        var restored = MolecularStructureDocument.Deserialize(MolecularStructureDocument.Serialize(Water()));

        restored.Atoms.Count.ShouldBe(3);
        restored.Bonds.Count.ShouldBe(2);
    }

    [Fact]
    public void Serialize_WritesBondTypeAsText() =>
        MolecularStructureDocument.Serialize(Water()).ShouldContain("Single");

    private static MolecularStructure Water() => MolecularStructure.Create(
        [
            new AtomPlacement(ElementSymbol.Create("O").Value, 0, 0, 0),
            new AtomPlacement(ElementSymbol.Create("H").Value, 1, 1, 0),
            new AtomPlacement(ElementSymbol.Create("H").Value, -1, 1, 0)
        ],
        [new Bond(0, 1, BondType.Single), new Bond(0, 2, BondType.Single)]).Value;
}
