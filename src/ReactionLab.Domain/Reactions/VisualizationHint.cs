using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public sealed record VisualizationHint
{
    public const int MaximumPresetKeyLength = 50;

    public const int MaximumDurationMilliseconds = 60000;

    public static readonly Error PresetKeyTooLong = Error.Validation(
        "VisualizationHint.PresetKeyTooLong",
        $"Effect preset key must not exceed {MaximumPresetKeyLength} characters.")
        .WithArgs(("max", MaximumPresetKeyLength));

    public static readonly Error InvalidDuration = Error.Validation(
        "VisualizationHint.InvalidDuration",
        $"Animation duration must be between 1 and {MaximumDurationMilliseconds} ms.")
        .WithArgs(("min", 1), ("max", MaximumDurationMilliseconds));

    private VisualizationHint(string? presetKey, int? durationMilliseconds)
    {
        PresetKey = presetKey;
        DurationMilliseconds = durationMilliseconds;
    }

    public string? PresetKey { get; }

    public int? DurationMilliseconds { get; }

    public static VisualizationHint None { get; } = new(null, null);

    public static Result<VisualizationHint> Create(string? presetKey, int? durationMilliseconds)
    {
        if (durationMilliseconds is { } duration && duration is <= 0 or > MaximumDurationMilliseconds)
        {
            return InvalidDuration;
        }

        if (string.IsNullOrWhiteSpace(presetKey))
        {
            return new VisualizationHint(null, durationMilliseconds);
        }

        var trimmed = presetKey.Trim();

        return trimmed.Length > MaximumPresetKeyLength
            ? PresetKeyTooLong
            : new VisualizationHint(trimmed, durationMilliseconds);
    }


}
