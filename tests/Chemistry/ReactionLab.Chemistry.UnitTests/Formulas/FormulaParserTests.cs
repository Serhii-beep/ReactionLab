using ReactionLab.Chemistry.Formulas;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Formulas;

public sealed class FormulaParserTests
{
    private static ChemicalComposition Parse(string formula)
    {
        FormulaParser.TryParse(formula, out var composition, out var error)
            .ShouldBeTrue($"'{formula}' was refused as {error}");

        return composition;
    }

    [Theory]
    [InlineData("H2O", "H2O")]
    [InlineData("NaCl", "ClNa")]
    [InlineData("O2", "O2")]
    [InlineData("Fe", "Fe")]
    [InlineData("C6H12O6", "C6H12O6")]
    [InlineData("C2H5OH", "C2H6O")]
    [InlineData("Ca(OH)2", "CaH2O2")]
    [InlineData("Pb(NO3)2", "N2O6Pb")]
    [InlineData("Al2(SO4)3", "Al2O12S3")]
    [InlineData("K4[Fe(CN)6]", "C6FeK4N6")]
    public void TryParse_RendersHillNotation(string formula, string hill) =>
        Parse(formula).Hill.ShouldBe(hill);

    [Theory]
    [InlineData("SO4^2-", -2)]
    [InlineData("Ca^2+", 2)]
    [InlineData("Cl^-", -1)]
    [InlineData("Na^+", 1)]
    [InlineData("NH4^+", 1)]
    [InlineData("H2O", 0)]
    public void TryParse_ReadsTheTrailingCharge(string formula, int charge) =>
        Parse(formula).Charge.ShouldBe(charge);

    [Fact]
    public void TryParse_ChargeDoesNotDisturbTheComposition()
    {
        var ion = Parse("SO4^2-");

        ion.Hill.ShouldBe("O4S");
        ion.TotalAtoms.ShouldBe(5);
    }

    [Fact]
    public void TryParse_IgnoresWhiteSpace() =>
        Parse("   H2O   ").Hill.ShouldBe("H2O");

    [Theory]
    [InlineData(null, FormulaError.Empty)]
    [InlineData("", FormulaError.Empty)]
    [InlineData("   ", FormulaError.Empty)]
    [InlineData("h2o", FormulaError.Malformed)]
    [InlineData("2H2O", FormulaError.Malformed)]
    [InlineData("H2O!", FormulaError.Malformed)]
    [InlineData("(H2O", FormulaError.UnbalancedGroup)]
    [InlineData("H2O)", FormulaError.UnbalancedGroup)]
    [InlineData("Ca(OH]2", FormulaError.UnbalancedGroup)]
    [InlineData("H0", FormulaError.InvalidCount)]
    [InlineData("H01", FormulaError.InvalidCount)]
    [InlineData("H2O^", FormulaError.InvalidCharge)]
    [InlineData("H2O^2", FormulaError.InvalidCharge)]
    [InlineData("H2O^0-", FormulaError.InvalidCharge)]
    [InlineData("^2-", FormulaError.Empty)]
    [InlineData("H2()", FormulaError.Malformed)]
    [InlineData("H2[]", FormulaError.Malformed)]
    [InlineData("Ca(())2", FormulaError.Malformed)]
    public void TryParse_RefusesMalformedInput(string? formula, FormulaError expected)
    {
        FormulaParser.TryParse(formula, out _, out var error).ShouldBeFalse();
        error.ShouldBe(expected);
    }

    [Fact]
    public void TryParse_RefusesTooLongFormula()
    {
        FormulaParser.TryParse(new string('H', FormulaParser.MaximumLength + 1), out _, out var error)
            .ShouldBeFalse();

        error.ShouldBe(FormulaError.TooLong);
    }
}
