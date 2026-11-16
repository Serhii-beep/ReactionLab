using System.Runtime.InteropServices;
using System.Text;
using ReactionLab.Chemistry.Interop;

namespace ReactionLab.Chemistry.Native;

internal static unsafe class ChemistryExports
{
    private const int CurrentInterfaceVersion = 1;

    [UnmanagedCallersOnly(EntryPoint = "chemistry_interface_version")]
    private static int InterfaceVersion() => CurrentInterfaceVersion;

    [UnmanagedCallersOnly(EntryPoint = "chemistry_engine_load")]
    private static ChemistryStatus LoadEngine(
        byte* elementsJson,
        nuint elementsJsonLength,
        byte* referenceJson,
        nuint referenceJsonLength,
        nint* engine,
        ChemistryText* failure)
    {
        try
        {
            var loaded = ChemistryEngine.Load(
                TextOf(elementsJson, elementsJsonLength),
                TextOf(referenceJson, referenceJsonLength));

            *engine = GCHandle.ToIntPtr(GCHandle.Alloc(loaded));

            return ChemistryStatus.Succeeded;
        }
        catch (Exception exception)
        {
            *failure = ChemistryText.Of(exception.Message);

            return ChemistryStatus.Failed;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "chemistry_engine_free")]
    private static void FreeEngine(nint engine)
    {
        if (engine != 0)
        {
            GCHandle.FromIntPtr(engine).Free();
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "chemistry_parse_formula")]
    private static ChemistryStatus ParseFormula(byte* formula, nuint formulaLength, ChemistryText* answer) =>
        Answer(formula, formulaLength, answer, ChemistryEngine.ParseFormula);

    [UnmanagedCallersOnly(EntryPoint = "chemistry_molar_mass_of")]
    private static ChemistryStatus MolarMassOf(
        nint engine,
        byte* formula,
        nuint formulaLength,
        ChemistryText* answer) =>
        Answer(engine, formula, formulaLength, answer, static (loaded, text) => loaded.MolarMassOf(text));

    [UnmanagedCallersOnly(EntryPoint = "chemistry_balance")]
    private static ChemistryStatus Balance(byte* request, nuint requestLength, ChemistryText* answer) =>
        Answer(request, requestLength, answer, ChemistryEngine.Balance);

    [UnmanagedCallersOnly(EntryPoint = "chemistry_assemble")]
    private static ChemistryStatus Assemble(
        nint engine,
        byte* request,
        nuint requestLength,
        ChemistryText* answer) =>
        Answer(engine, request, requestLength, answer, static (loaded, text) => loaded.Assemble(text));

    [UnmanagedCallersOnly(EntryPoint = "chemistry_describe_sketch")]
    private static ChemistryStatus DescribeSketch(
        nint engine,
        byte* request,
        nuint requestLength,
        ChemistryText* answer) =>
        Answer(engine, request, requestLength, answer, static (loaded, text) => loaded.DescribeSketch(text));

    [UnmanagedCallersOnly(EntryPoint = "chemistry_structure_key")]
    private static ChemistryStatus StructureKey(byte* request, nuint requestLength, ChemistryText* answer) =>
        Answer(request, requestLength, answer, ChemistryEngine.StructureKey);

    [UnmanagedCallersOnly(EntryPoint = "chemistry_detect_symmetry")]
    private static ChemistryStatus DetectSymmetry(byte* request, nuint requestLength, ChemistryText* answer) =>
        Answer(request, requestLength, answer, ChemistryEngine.DetectSymmetry);

    [UnmanagedCallersOnly(EntryPoint = "chemistry_predict")]
    private static ChemistryStatus Predict(
        nint engine,
        byte* request,
        nuint requestLength,
        ChemistryText* answer) =>
        Answer(engine, request, requestLength, answer, static (loaded, text) => loaded.Predict(text));

    [UnmanagedCallersOnly(EntryPoint = "chemistry_describe_conditions")]
    private static ChemistryStatus DescribeConditions(
        nint engine,
        byte* request,
        nuint requestLength,
        ChemistryText* answer) =>
        Answer(engine, request, requestLength, answer, static (loaded, text) => loaded.DescribeConditions(text));

    [UnmanagedCallersOnly(EntryPoint = "chemistry_text_free")]
    private static void FreeText(byte* bytes) => NativeMemory.Free(bytes);

    private static ChemistryStatus Answer(
        byte* input,
        nuint inputLength,
        ChemistryText* answer,
        Func<string, string> operation)
    {
        try
        {
            *answer = ChemistryText.Of(operation(TextOf(input, inputLength)));

            return ChemistryStatus.Succeeded;
        }
        catch (Exception ex)
        {
            *answer = ChemistryText.Of(ex.Message);

            return ChemistryStatus.Failed;
        }
    }

    private static ChemistryStatus Answer(
        nint engine,
        byte* input,
        nuint inputLength,
        ChemistryText* answer,
        Func<ChemistryEngine, string, string> operation)
    {
        try
        {
            var loaded = (ChemistryEngine)GCHandle.FromIntPtr(engine).Target!;

            *answer = ChemistryText.Of(operation(loaded, TextOf(input, inputLength)));

            return ChemistryStatus.Succeeded;
        }
        catch (Exception ex)
        {
            *answer = ChemistryText.Of(ex.Message);

            return ChemistryStatus.Failed;
        }
    }

    private static string TextOf(byte* bytes, nuint length) => Encoding.UTF8.GetString(bytes, checked((int)length));
}
