namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ConditionsCurveEntry(
    IReadOnlyList<decimal> Kelvins,
    IReadOnlyList<double?>? Conversions,
    IReadOnlyList<double?>? ConversionsAtTenfoldPressure,
    IReadOnlyList<double?>? ConversionsAtTenthPressure,
    IReadOnlyList<double>? Log10SharesOverBarrier);
