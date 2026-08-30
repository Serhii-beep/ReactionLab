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

    [Theory]
    [InlineData("HCl", "Cl", 1)]
    [InlineData("H2SO4", "SO4", 2)]
    [InlineData("H3PO4", "PO4", 3)]
    [InlineData("H2S", "S", 2)]
    public void TryReadAcid_TakesTheChargeFromTheLeadingHydrogens(
        string formula, string anion, int charge)
    {
        IonicFormula.TryReadAcid(formula, out var read, out var readCharge).ShouldBeTrue(formula);

        read.ShouldBe(anion);
        readCharge.ShouldBe(charge);
    }

    [Theory]
    [InlineData("H2O")]
    [InlineData("H2O2")]
    [InlineData("He")]
    [InlineData("Hg")]
    [InlineData("NaOH")]
    [InlineData("C2H4O2")]
    public void TryReadAcid_RefusesWhatIsNotWrittenAsAnAcid(string formula) =>
        IonicFormula.TryReadAcid(formula, out _, out _).ShouldBeFalse();

    [Theory]
    [InlineData("NaOH", "Na", 1)]
    [InlineData("Ca(OH)2", "Ca", 2)]
    [InlineData("Al(OH)3", "Al", 3)]
    [InlineData("NH4OH", "NH4", 1)]
    public void TryReadBase_TakesTheChargeFromTheHydroxideCount(
        string formula, string cation, int charge)
    {
        IonicFormula.TryReadBase(formula, out var read, out var readCharge).ShouldBeTrue(formula);

        read.ShouldBe(cation);
        readCharge.ShouldBe(charge);
    }

    [Theory]
    [InlineData("H2O")]
    [InlineData("HCl")]
    [InlineData("OH")]
    public void TryReadBase_RefusesWhatIsNotWrittenAsAHydroxide(string formula) =>
        IonicFormula.TryReadBase(formula, out _, out _).ShouldBeFalse();
}
