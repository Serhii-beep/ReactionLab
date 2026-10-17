using System.Text.Json.Serialization;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SpeciesEnthalpyEntry(
    string Formula,
    Phase Phase,
    [property: JsonPropertyName("enthalpy")] decimal FormationEnthalpyKilojoulesPerMole);
