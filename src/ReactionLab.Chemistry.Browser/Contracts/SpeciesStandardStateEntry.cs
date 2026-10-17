using System.Text.Json.Serialization;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SpeciesStandardStateEntry(
    string Formula,
    Phase Phase,
    [property: JsonPropertyName("enthalpy")] decimal FormationEnthalpyKilojoulesPerMole,
    [property: JsonPropertyName("entropy")] decimal? StandardEntropyJoulesPerMoleKelvin = null);
