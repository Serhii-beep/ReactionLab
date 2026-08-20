using ReactionLab.Domain.Common;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Domain.Reactions;

public sealed class ReactionContent : ITranslatableContent<ReactionContent>
{
    public const int MaximumNameLength = 200;

    public static readonly Error NameRequired = Error.Validation(
        "ReactionContent.NameRequired",
        "Reaction name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "ReactionContent.NameTooLong",
        $"Reaction name must not exceed {MaximumNameLength} characters.")
        .WithArgs(("max", MaximumNameLength));

    private ReactionContent(
        string name,
        string? description,
        string? mechanism,
        string? safetyWarnings,
        IReadOnlyList<string> realWorldExamples)
    {
        Name = name;
        Description = description;
        Mechanism = mechanism;
        SafetyWarnings = safetyWarnings;
        RealWorldExamples = realWorldExamples;
    }

    public string Name { get; }

    public string? Description { get; }

    public string? Mechanism { get; }

    public string? SafetyWarnings { get; }

    public IReadOnlyList<string> RealWorldExamples { get; }

    public static Result<ReactionContent> Create(
        string? name,
        string? description = null,
        string? mechanism = null,
        string? safetyWarnings = null,
        IEnumerable<string>? realWorldExamples = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameRequired;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > MaximumNameLength)
        {
            return NameTooLong;
        }

        return new ReactionContent(
            trimmed,
            TextNormalizer.Clean(description),
            TextNormalizer.Clean(mechanism),
            TextNormalizer.Clean(safetyWarnings),
            TextNormalizer.CleanAll(realWorldExamples));
    }

    public ReactionContent WithFallback(ReactionContent fallback) =>
        new(Name,
            Description ?? fallback.Description,
            Mechanism ?? fallback.Mechanism,
            SafetyWarnings ?? fallback.SafetyWarnings,
            RealWorldExamples.Count > 0 ? RealWorldExamples : fallback.RealWorldExamples);
}
