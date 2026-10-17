using System.Text.Json.Serialization;

namespace ReactionLab.Chemistry.Browser.Contracts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(IReadOnlyList<ElementEntry>))]
[JsonSerializable(typeof(ChemistryReferenceDocument))]
[JsonSerializable(typeof(FormulaResult))]
[JsonSerializable(typeof(MolarMassResult))]
[JsonSerializable(typeof(BalanceRequest))]
[JsonSerializable(typeof(BalanceResult))]
[JsonSerializable(typeof(SkeletonRequest))]
[JsonSerializable(typeof(AssemblyResult))]
[JsonSerializable(typeof(SketchRequest))]
[JsonSerializable(typeof(SketchDescription))]
[JsonSerializable(typeof(SymmetryRequest))]
[JsonSerializable(typeof(SymmetryResult))]
[JsonSerializable(typeof(PredictionRequest))]
[JsonSerializable(typeof(PredictionResult))]
[JsonSerializable(typeof(ConditionsRequest))]
[JsonSerializable(typeof(ConditionsResult))]
internal sealed partial class ChemistryJsonContext : JsonSerializerContext;
