using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record AqueousIonEntry(
    string Formula,
    int Charge,
    [property: JsonPropertyName("enthalpy")] decimal FormationEnthalpyKilojoulesPerMole);
