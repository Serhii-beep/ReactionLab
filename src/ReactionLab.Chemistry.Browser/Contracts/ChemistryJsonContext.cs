using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Browser.Contracts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Dictionary<string, decimal>))]
[JsonSerializable(typeof(IReadOnlyList<AtomicGeometryEntry>))]
[JsonSerializable(typeof(FormulaResult))]
[JsonSerializable(typeof(MolarMassResult))]
[JsonSerializable(typeof(BalanceRequest))]
[JsonSerializable(typeof(BalanceResult))]
[JsonSerializable(typeof(SkeletonRequest))]
[JsonSerializable(typeof(AssemblyResult))]
internal sealed partial class ChemistryJsonContext : JsonSerializerContext;
