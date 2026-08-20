namespace ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

public sealed record ReactionRecord
{
    public required string Key { get; init; }

    public required string Type { get; init; }

    public required int Difficulty { get; init; }

    public bool IsReversible { get; init; }

    public decimal? EnthalpyKjPerMol { get; init; }

    public decimal? ActivationEnergyKjPerMol { get; init; }

    public decimal? TemperatureK { get; init; }

    public string? Catalyst { get; init; }

    public string? EffectPreset { get; init; }

    public int? AnimationDurationMs { get; init; }

    public required List<ParticipantRecord> Participants { get; init; }

    public List<string>? Tags { get; init; }

    public required Dictionary<string, ReactionText> Translations { get; init; }

    public sealed record ReactionText(
        string Name,
        string? Description,
        string? Mechanism,
        string? SafetyWarnings,
        List<string>? RealWorldExamples);

    public sealed record ParticipantRecord(string Formula, string Role, int Coefficient, string? State, string? Substance);
}
