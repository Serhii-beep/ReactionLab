using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record AtomicGeometryEntry(
    string Symbol,
    int ValenceElectrons,
    [property: JsonPropertyName("single")] int SingleBondRadiusPicometers,
    [property: JsonPropertyName("double")] int? DoubleBondRadiusPicometers = null,
    [property: JsonPropertyName("triple")] int? TripleBondRadiusPicometers = null,
    [property: JsonPropertyName("aromatic")] int? AromaticBondRadiusPicometers = null);
