using ReactionLab.Chemistry.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Ions;

public sealed class IonicFormulaTests
{
    [Theory]
    [InlineData("Na", 1, "Cl", 1, "NaCl")]
    [InlineData("Ca", 2, "Cl", 1, "CaCl2")]
    [InlineData("Na", 1, "SO4", 2, "Na2SO4")]
    [InlineData("Ca", 2, "SO4", 2, "CaSO4")]
    [InlineData("Al", 3, "SO4", 2, "Al2(SO4)3")]
    [InlineData("Ca", 2, "OH", 1, "Ca(OH)2")]
    [InlineData("Na", 1, "OH", 1, "NaOH")]
    [InlineData("NH4", 1, "Cl", 1, "NH4Cl")]
    [InlineData("NH4", 1, "SO4", 2, "(NH4)2SO4")]
    [InlineData("Pb", 2, "NO3", 1, "Pb(NO3)2")]
    public void Combine_WritesTheSmallestNeutralRatio(
        string cation, int cationCharge, string anion, int anionCharge, string formula) =>
        IonicFormula.Combine(cation, cationCharge, anion, anionCharge).ShouldBe(formula);
}
