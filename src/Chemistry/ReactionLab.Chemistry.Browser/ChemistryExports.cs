using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ReactionLab.Chemistry.Interop;

namespace ReactionLab.Chemistry.Browser;

[SupportedOSPlatform("browser")]
internal static partial class ChemistryExports
{
    private static ChemistryEngine? _engine;

    private static ChemistryEngine Engine =>
        _engine ?? throw new InvalidOperationException("The chemistry reference data is not loaded.");

    [JSExport]
    internal static void LoadReferenceData(string elementsJson, string referenceJson) =>
        _engine = ChemistryEngine.Load(elementsJson, referenceJson);

    [JSExport]
    internal static string ParseFormula(string formula) => ChemistryEngine.ParseFormula(formula);

    [JSExport]
    internal static string MolarMassOf(string formula) => Engine.MolarMassOf(formula);

    [JSExport]
    internal static string Balance(string requestJson) => ChemistryEngine.Balance(requestJson);

    [JSExport]
    internal static string Assemble(string requestJson) => Engine.Assemble(requestJson);

    [JSExport]
    internal static string DescribeSketch(string requestJson) => Engine.DescribeSketch(requestJson);

    [JSExport]
    internal static string StructureKey(string requestJson) => ChemistryEngine.StructureKey(requestJson);

    [JSExport]
    internal static string DetectSymmetry(string requestJson) => ChemistryEngine.DetectSymmetry(requestJson);

    [JSExport]
    internal static string Predict(string requestJson) => Engine.Predict(requestJson);

    [JSExport]
    internal static string DescribeConditions(string requestJson) => Engine.DescribeConditions(requestJson);
}
