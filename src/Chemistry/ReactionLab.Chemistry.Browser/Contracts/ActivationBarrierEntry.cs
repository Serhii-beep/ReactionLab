using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ActivationBarrierEntry(
    string Family,
    [property: JsonPropertyName("intrinsic")] decimal IntrinsicKilojoulesPerMole,
    [property: JsonPropertyName("transfer")] decimal TransferCoefficient,
    [property: JsonPropertyName("minimum")] decimal MinimumKilojoulesPerMole);
