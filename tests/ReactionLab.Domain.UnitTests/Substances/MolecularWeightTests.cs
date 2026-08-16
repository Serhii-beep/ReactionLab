using ReactionLab.Domain.Substances;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Substances;

public sealed class MolecularWeightTests
{
    [Fact]
    public void Create_AcceptsPositiveWeight() =>
        MolecularWeight.Create(18.015m).Value.GramsPerMole.ShouldBe(18.015m);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveWeight(decimal grams) =>
        MolecularWeight.Create(grams).Error.ShouldBe(MolecularWeight.NotPositive);

    [Fact]
    public void Create_RejectsImplausibleWeight() =>
        MolecularWeight.Create(500000m).Error.ShouldBe(MolecularWeight.Implausible);
}
