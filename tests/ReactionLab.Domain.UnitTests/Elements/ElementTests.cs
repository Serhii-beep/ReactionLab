using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Elements.Events;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class ElementTests
{
    [Fact]
    public void Create_ProducesAnElementAnRaisesTheCreatedEvent()
    {
        var element = CreateElement().Value;

        element.Symbol.Value.ShouldBe("H");
        element.Name.ShouldBe("Hydrogen");
        element.Id.Value.ShouldNotBe(Guid.Empty);
        element.DomainEvents.OfType<ElementCreated>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name)
    {
        var result = CreateElement(name: name);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Element.NameRequired);
    }

    [Fact]
    public void Create_RejectsOverlongName()
    {
        var result = CreateElement(name: new string('x', Element.MaximumNameLength + 1));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Element.NameTooLong);
    }

    [Fact]
    public void Create_TrimsName() =>
        CreateElement(name: "   Hydrogen   ").Value.Name.ShouldBe("Hydrogen");

    [Theory]
    [InlineData(ElementCategory.Lanthanide)]
    [InlineData(ElementCategory.Actinide)]
    public void Create_RejectsFBlockElementWithAGroup(ElementCategory category)
    {
        var result = CreateElement(category: category, position: PeriodicPosition.Create(6, 3).Value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Element.FBlockCannotHaveGroup);
    }

    [Theory]
    [InlineData(ElementCategory.Lanthanide)]
    [InlineData(ElementCategory.Actinide)]
    public void Create_AcceptsFBlockElementWithoutAGroup(ElementCategory category)
    {
        var result = CreateElement(category: category, position: PeriodicPosition.Create(6, null).Value);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void DescribePhysicalProperties_RejectsBoilingPointBelowMeltingPoint()
    {
        var element = CreateElement().Value;

        var result = element.DescribePhysicalProperties(
            electronegativity: null,
            radii: null,
            meltingPoint: Temperature.FromKelvin(500).Value,
            boilingPoint: Temperature.FromKelvin(400).Value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Element.BoilingPointBelowMeltingPoint);
        element.MeltingPoint.ShouldBeNull();
    }

    [Fact]
    public void DescribePhysicalProperties_AcceptsOrderedTemperatures()
    {
        var element = CreateElement().Value;

        var result = element.DescribePhysicalProperties(
            Electronegativity.Create(2.20m).Value,
            AtomicRadii.Create(31m, 120m).Value,
            Temperature.FromKelvin(13.99m).Value,
            Temperature.FromKelvin(20.271m).Value);

        result.IsSuccess.ShouldBeTrue();
        element.Electronegativity!.Pauling.ShouldBe(2.20m);
        element.BoilingPoint!.Kelvin.ShouldBe(20.271m);
    }

    [Fact]
    public void DescribeDiscovery_TrimsAndDiscardsBlankFacts()
    {
        var element = CreateElement().Value;

        element.DescribeDiscovery("1s1", "   Cavendish, 1766   ", ["   Most abundant   ", "", "   "]);

        element.DiscoveryInfo.ShouldBe("Cavendish, 1766");
        element.InterestingFacts.ShouldHaveSingleItem().ShouldBe("Most abundant");
    }

    [Fact]
    public void InterestingFacts_CannotBeMutatedByCallers()
    {
        var element = CreateElement().Value;
        element.DescribeDiscovery(null, null, ["fact"]);

        (element.InterestingFacts as ICollection<string>)?.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void UpdateAppearance_ReplacesColorAndRadii()
    {
        var element = CreateElement().Value;

        element.UpdateAppearance(HexColor.Create("#4FC3F7").Value, AtomicRadii.Create(40m).Value);

        element.DisplayColor.Value.ShouldBe("#4FC3F7");
        element.Radii!.CovalentPicometers.ShouldBe(40m);
    }

    private static Result<Element> CreateElement(
        string? name = "Hydrogen",
        ElementCategory category = ElementCategory.NonMetal,
        PeriodicPosition? position = null) =>
        Element.Create(
            AtomicNumber.Create(1).Value,
            ElementSymbol.Create("H").Value,
            name,
            AtomicMass.Create(1.008m).Value,
            category,
            position ?? PeriodicPosition.Create(1, 1).Value,
            MatterState.Gas,
            HexColor.Create("#FFFFFF").Value);
}
