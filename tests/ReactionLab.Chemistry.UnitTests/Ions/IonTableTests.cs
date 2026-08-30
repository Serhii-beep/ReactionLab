using ReactionLab.Chemistry.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Ions;

public sealed class IonTableTests
{
    [Theory]
    [InlineData("NaCl", "Na", 1, "Cl", -1)]
    [InlineData("Na2SO4", "Na", 1, "SO4", -2)]
    [InlineData("CaCO3", "Ca", 2, "CO3", -2)]
    [InlineData("Fe2O3", "Fe", 3, "O", -2)]
    [InlineData("FeSO4", "Fe", 2, "SO4", -2)]
    [InlineData("KMnO4", "K", 1, "MnO4", -1)]
    [InlineData("NH4Cl", "NH4", 1, "Cl", -1)]
    [InlineData("Pb(NO3)2", "Pb", 2, "NO3", -1)]
    [InlineData("Al2O3", "Al", 3, "O", -2)]
    [InlineData("MgO", "Mg", 2, "O", -2)]
    public void TrySplit_ReadsTheIonsBackOutOfASalt(
        string formula, string cation, int cationCharge, string anion, int anionCharge)
    {
        TestIons.Table.TrySplit(formula, out var readCation, out var readAnion).ShouldBeTrue(formula);

        readCation.ShouldBe(new Ion(cation, cationCharge));
        readAnion.ShouldBe(new Ion(anion, anionCharge));
    }

    [Theory]
    [InlineData("H2O")]
    [InlineData("CH4")]
    [InlineData("C6H12O6")]
    [InlineData("CH3COONa")]
    public void TrySplit_RefusesWhatIsNotACationAndAnAnion(string formula) =>
        TestIons.Table.TrySplit(formula, out _, out _).ShouldBeFalse();

    [Theory]
    [InlineData("NaCl", Solubility.Soluble, "groupOneAndAmmonium")]
    [InlineData("Na2CO3", Solubility.Soluble, "groupOneAndAmmonium")]
    [InlineData("NH4NO3", Solubility.Soluble, "groupOneAndAmmonium")]
    [InlineData("AgNO3", Solubility.Soluble, "nitratesAndAcetates")]
    [InlineData("CaCl2", Solubility.Soluble, "halides")]
    [InlineData("AgCl", Solubility.Insoluble, "halides")]
    [InlineData("PbI2", Solubility.Insoluble, "halides")]
    [InlineData("CuSO4", Solubility.Soluble, "sulfates")]
    [InlineData("BaSO4", Solubility.Insoluble, "sulfates")]
    [InlineData("CaSO4", Solubility.Insoluble, "sulfates")]
    [InlineData("Mg(OH)2", Solubility.Insoluble, "hydroxides")]
    [InlineData("Ba(OH)2", Solubility.Soluble, "hydroxides")]
    [InlineData("CaCO3", Solubility.Insoluble, "carbonatesAndTheRest")]
    [InlineData("Fe2O3", Solubility.Insoluble, "carbonatesAndTheRest")]
    public void SolubilityOf_TakesTheFirstRuleThatMatches(
        string salt, Solubility solubility, string rule)
    {
        TestIons.Table.TrySplit(salt, out var cation, out var anion).ShouldBeTrue(salt);

        TestIons.Table.SolubilityOf(cation, anion, out var decided).ShouldBe(solubility, salt);
        decided.ShouldBe(rule, salt);
    }

    [Fact]
    public void SolubilityOf_ReportsUnknownWhenTheRulesHaveNoCatch()
    {
        var sparse = new IonTable(
            [new Ion("Na", 1)],
            [new Ion("Cl", -1)],
            [new SolubilityRule("onlyNitrates", Solubility.Soluble, Anions: ["NO3"])],
            [],
            []);

        sparse.SolubilityOf(new Ion("Na", 1), new Ion("Cl", -1), out var rule)
            .ShouldBe(Solubility.Unknown);

        rule.ShouldBe("unmatched");
    }

    [Theory]
    [InlineData("HCl", "Cl", -1)]
    [InlineData("H2SO4", "SO4", -2)]
    [InlineData("H3PO4", "PO4", -3)]
    [InlineData("H2S", "S", -2)]
    public void TryReadAcid_TakesTheAnionFromTheTable(
        string formula, string anion, int charge)
    {
        TestIons.Table.TryReadAcid(formula, out var read).ShouldBeTrue(formula);

        read.ShouldBe(new Ion(anion, charge));
    }

    [Theory]
    [InlineData("H2O")]
    [InlineData("H2O2")]
    [InlineData("He")]
    [InlineData("Hg")]
    [InlineData("NaOH")]
    [InlineData("C2H4O2")]
    public void TryReadAcid_RefusesWhatIsNotWrittenAsAnAcid(string formula) =>
        TestIons.Table.TryReadAcid(formula, out _).ShouldBeFalse();

    [Theory]
    [InlineData("NaOH", "Na", 1)]
    [InlineData("Ca(OH)2", "Ca", 2)]
    [InlineData("Al(OH)3", "Al", 3)]
    [InlineData("NH4OH", "NH4", 1)]
    public void TryReadBase_TakesTheChargeFromTheHydroxideCount(
        string formula, string cation, int charge)
    {
        TestIons.Table.TryReadBase(formula, out var read).ShouldBeTrue(formula);

        read.ShouldBe(new Ion(cation, charge));
    }

    [Theory]
    [InlineData("H2O")]
    [InlineData("HCl")]
    [InlineData("OH")]
    public void TryReadBase_RefusesWhatIsNotWrittenAsAHydroxide(string formula) =>
        TestIons.Table.TryReadBase(formula, out _).ShouldBeFalse();
}
