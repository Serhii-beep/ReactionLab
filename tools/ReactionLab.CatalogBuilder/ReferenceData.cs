using System.Text.Json;
using ReactionLab.Chemistry.Ions;

namespace ReactionLab.CatalogBuilder;

internal static class ReferenceData
{
    public static IonTable ReadIons(string path)
    {
        var document = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
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
        var document = JsonDocument.Parse(File.ReadAllText(path)).RootElement;

        return new ActivitySeries(
        [
            .. document.GetProperty("metals").EnumerateArray().Select(metal => new ActiveMetal(
                metal.GetProperty("symbol").GetString()!,
                metal.GetProperty("charge").GetInt32(),
                Enum.Parse<WaterReactivity>(metal.GetProperty("water").GetString()!)))
        ]);
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
}
