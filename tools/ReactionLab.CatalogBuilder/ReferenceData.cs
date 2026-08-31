using System.Text.Json;
using ReactionLab.Chemistry.Geometry;
using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.CatalogBuilder;

internal static class ReferenceData
{
    public static IonTable ReadIons(string path)
    {
        var document = Document(path);
        var behaviors = document.GetProperty("behaviors");

        return new IonTable(
            Ions(document, "cations"),
            Ions(document, "anions"),
            [.. document.GetProperty("solubility").EnumerateArray().Select(rule => new SolubilityRule(
                rule.GetProperty("code").GetString()!,
                Enum.Parse<Solubility>(rule.GetProperty("solubility").GetString()!),
                Optional(rule, "cations"),
                Optional(rule, "anions"),
                Optional(rule, "exceptCations")))],
           new IonBehaviors(
               Names(behaviors, "thermallyStableCations"),
               Names(behaviors, "oxidizingAnions"),
               Names(behaviors, "unstableAcidAnions"),
               Names(behaviors, "hydrolyzingAnions")));
    }

    public static ActivitySeries ReadActivitySeries(string path)
    {
        var document = Document(path);

        return new ActivitySeries(
        [
            .. document.GetProperty("metals").EnumerateArray().Select(metal => new ActiveMetal(
                metal.GetProperty("symbol").GetString()!,
                metal.GetProperty("charge").GetInt32(),
                Enum.Parse<WaterReactivity>(metal.GetProperty("water").GetString()!)))
        ]);
    }

    public static List<SpeciesEnthalpy> ReadSpeciesEnthalpy(string path) =>
    [
        .. Document(path).GetProperty("species").EnumerateArray().Select(entry => new SpeciesEnthalpy(
            entry.GetProperty("formula").GetString()!,
            Enum.Parse<Phase>(entry.GetProperty("phase").GetString()!),
            entry.GetProperty("enthalpy").GetDecimal()))
    ];

    public static List<AqueousIon> ReadAqueousIons(string path) =>
    [
        .. Document(path).GetProperty("aqueousIons").EnumerateArray().Select(entry => new AqueousIon(
            new Ion(entry.GetProperty("formula").GetString()!, entry.GetProperty("charge").GetInt32()),
            entry.GetProperty("enthalpy").GetDecimal()))
    ];

    public static List<ActivationBarrier> ReadBarriers(string path) =>
    [
        .. Document(path).GetProperty("barriers").EnumerateArray().Select(entry => new ActivationBarrier(
            entry.GetProperty("family").GetString()!,
            entry.GetProperty("intrinsic").GetDecimal(),
            entry.GetProperty("transfer").GetDecimal(),
            entry.GetProperty("minimum").GetDecimal()))
    ];

    public static Dictionary<string, AtomicGeometry> ReadAtomicGeometry(string path)
    {
        var table = new Dictionary<string, AtomicGeometry>(StringComparer.Ordinal);

        foreach (var entry in Document(path).GetProperty("elements").EnumerateArray())
        {
            table[entry.GetProperty("symbol").GetString()!] = new AtomicGeometry(
                entry.GetProperty("valenceElectrons").GetInt32(),
                entry.GetProperty("single").GetInt32(),
                Optional(entry, "double"),
                Optional(entry, "triple"),
                Optional(entry, "aromatic"));
        }

        return table;

        static int? Optional(JsonElement entry, string name) =>
            entry.TryGetProperty(name, out var value) ? value.GetInt32() : null;
    }

    private static List<Ion> Ions(JsonElement document, string name) =>
    [
        .. document.GetProperty(name).EnumerateArray().Select(ion => new Ion(
            ion.GetProperty("formula").GetString()!,
            ion.GetProperty("charge").GetInt32()))
    ];

    private static List<string> Names(JsonElement document, string name) =>
        document.TryGetProperty(name, out var array)
            ? [.. array.EnumerateArray().Select(value => value.GetString()!)]
            : [];

    private static List<string>? Optional(JsonElement rule, string name) =>
        rule.TryGetProperty(name, out var array)
            ? [.. array.EnumerateArray().Select(value => value.GetString()!)]
            : null;

    private static JsonElement Document(string path) =>
        JsonDocument.Parse(File.ReadAllText(path)).RootElement;
}
