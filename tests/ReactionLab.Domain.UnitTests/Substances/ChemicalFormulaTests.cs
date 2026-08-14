using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Substances;

public sealed class ChemicalFormulaTests
{
    [Theory]
    [InlineData("H2O", "H2O")]
    [InlineData("CO2", "CO2")]
    [InlineData("NaCl", "ClNa")]
    [InlineData("C2H5OH", "C2H6O")]
    [InlineData("CH3COOH", "C2H4O2")]
    [InlineData("Ca(OH)2", "CaH2O2")]
    [InlineData("Mg(OH)2", "H2MgO2")]
    [InlineData("KMnO4", "KMnO4")]
    public void Create_ParsesAndCanonicalizesToHillNotation(string written, string expected) =>
        ChemicalFormula.Create(written).Value.Hill.ShouldBe(expected);

    [Fact]
    public void Create_PreservesTheWrittenForm() =>
        ChemicalFormula.Create("C2H5OH").Value.Value.ShouldBe("C2H5OH");

    [Fact]
    public void Composition_TalliesRepeatedElements()
    {
        var formula = ChemicalFormula.Create("CH3COOH").Value;

        formula.CountOf(ElementSymbol.Create("C").Value).ShouldBe(2);
        formula.CountOf(ElementSymbol.Create("H").Value).ShouldBe(4);
        formula.CountOf(ElementSymbol.Create("O").Value).ShouldBe(2);
    }

    [Fact]
    public void Composition_ExpandsParenthesizedGroups()
    {
        var formula = ChemicalFormula.Create("Ca(OH)2").Value;

        formula.CountOf(ElementSymbol.Create("Ca").Value).ShouldBe(1);
        formula.CountOf(ElementSymbol.Create("O").Value).ShouldBe(2);
        formula.CountOf(ElementSymbol.Create("H").Value).ShouldBe(2);
    }

    [Fact]
    public void Composition_HandlesNestedGroups()
    {
        var formula = ChemicalFormula.Create("K4(Fe(CN)6)").Value;

        formula.CountOf(ElementSymbol.Create("K").Value).ShouldBe(4);
        formula.CountOf(ElementSymbol.Create("Fe").Value).ShouldBe(1);
        formula.CountOf(ElementSymbol.Create("C").Value).ShouldBe(6);
        formula.CountOf(ElementSymbol.Create("N").Value).ShouldBe(6);
    }

    [Fact]
    public void Composition_IsOrderedByHillConvention()
    {
        var symbols = ChemicalFormula.Create("C2H5OH").Value.Composition
            .Select(quantity => quantity.Symbol.Value)
            .ToArray();

        symbols.ShouldBe(["C", "H", "O"]);
    }

    [Fact]
    public void Composition_OrdersAlphabeticallyWhenTHereIsNoCarbon()
    {
        var symbols = ChemicalFormula.Create("H2SO4").Value.Composition
            .Select(quantity => quantity.Symbol.Value)
            .ToArray();

        symbols.ShouldBe(["H", "O", "S"]);
    }

    [Fact]
    public void Create_DistinguishesTwoLetterSymbolsFromTwoElements()
    {
        ChemicalFormula.Create("Co").Value.Hill.ShouldBe("Co");
        ChemicalFormula.Create("CO").Value.Hill.ShouldBe("CO");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyInput(string? value) =>
        ChemicalFormula.Create(value).Error.ShouldBe(ChemicalFormula.Empty);

    [Theory]
    [InlineData("h2o")]
    [InlineData("H2O!")]
    [InlineData("2H2O")]
    public void Create_RejectsMalformedInput(string value) =>
        ChemicalFormula.Create(value).Error.ShouldBe(ChemicalFormula.Malformed);

    [Theory]
    [InlineData("Ca(OH2")]
    [InlineData("Ca)OH(2")]
    public void Create_RejectsUnbalancedParenthesis(string value) =>
        ChemicalFormula.Create(value).Error.ShouldBe(ChemicalFormula.UnbalancedParentheses);

    [Theory]
    [InlineData("H0")]
    [InlineData("H02")]
    public void Create_RejectsInvalidSubscripts(string value) =>
        ChemicalFormula.Create(value).Error.ShouldBe(ChemicalFormula.InvalidCount);

    [Fact]
    public void Equality_IsOnTheWrittenForm()
    {
        var written = ChemicalFormula.Create("C2H5OH").Value;
        var canonical = ChemicalFormula.Create("C2H6O").Value;

        written.ShouldNotBe(canonical);
        written.Hill.ShouldBe(canonical.Hill);
        ChemicalFormula.Create("C2H5OH").Value.ShouldBe(written);
    }
}
