using ReactionLab.Domain.Elements;
using ReactionLab.Domain.SharedKernel;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Converters;
using Shouldly;
using Xunit;

namespace ReactionLab.Infrastructure.UnitTests.Converters;

public sealed class ConverterTests
{
    [Fact]
    public void StronglyTypedId_RoundTrips()
    {
        var converter = new StronglyTypedIdConverter<ElementId>();
        var id = ElementId.New();

        var stored = converter.ConvertToProvider(id);

        stored.ShouldBe(id.Value);
        converter.ConvertFromProvider(stored).ShouldBe(id);
    }

    [Fact]
    public void StringBackedValueObject_RoundTrips()
    {
        var converter = new ElementSymbolConverter();
        var symbol = ElementSymbol.Create("H").Value;

        converter.ConvertToProvider(symbol).ShouldBe("H");
        converter.ConvertFromProvider("H").ShouldBe(symbol);
    }

    [Fact]
    public void DecimalBackedValueObject_RoundTripsWithoutLoosingPrecision()
    {
        var converter = new TemperatureConverter();
        var temperature = Temperature.FromKelvin(273.15m).Value;

        converter.ConvertToProvider(temperature).ShouldBe(273.15m);
        converter.ConvertFromProvider(273.15m).ShouldBe(temperature);
    }

    [Fact]
    public void ChemicalFormula_ReparsesTheCanonicalForm() =>
        ((ChemicalFormula)new ChemicalFormulaConverter().ConvertFromProvider("C6H6")!).Hill.ShouldBe("C6H6");

    [Fact]
    public void CorruptStoredValue_Throws() =>
        Should.Throw<InvalidOperationException>(
            () => new ElementSymbolConverter().ConvertFromProvider("test"));
}
