using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal sealed record ReactionContentDocument
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Mechanism { get; init; }

    public string? SafetyWarnings { get; init; }

    public IReadOnlyList<string>? RealWorldExamples { get; init; }

    public static ReactionContentDocument FromDomain(ReactionContent content) => new()
    {
        Name = content.Name,
        Description = content.Description,
        Mechanism = content.Mechanism,
        SafetyWarnings = content.SafetyWarnings,
        RealWorldExamples = content.RealWorldExamples.Count > 0 ? content.RealWorldExamples : null
    };

    public ReactionContent ToDomain() => PersistenceJson.Require(
        ReactionContent.Create(Name, Description, Mechanism, SafetyWarnings, RealWorldExamples), "reaction content");

    public static string Serialize(Translations<ReactionContent> translations) =>
        TranslationDocuments.Serialize(translations, FromDomain);

    public static Translations<ReactionContent> Deserialize(string json) =>
        TranslationDocuments.Deserialize<ReactionContent, ReactionContentDocument>(json, document => document.ToDomain());
}
