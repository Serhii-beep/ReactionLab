using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal sealed record ElementContentDocument
{
    public string Name { get; init; } = string.Empty;

    public string? DiscoveryInfo { get; init; }

    public IReadOnlyList<string>? InterestingFacts { get; init; }

    public static ElementContentDocument FromDomain(ElementContent content) => new()
    {
        Name = content.Name,
        DiscoveryInfo = content.DiscoveryInfo,
        InterestingFacts = content.InterestingFacts.Count > 0 ? content.InterestingFacts : null
    };

    public ElementContent ToDomain() => PersistenceJson.Require(
        ElementContent.Create(Name, DiscoveryInfo, InterestingFacts), "element content");

    public static string Serialize(Translations<ElementContent> translations) =>
        TranslationDocuments.Serialize(translations, FromDomain);

    public static Translations<ElementContent> Deserialize(string json) =>
        TranslationDocuments.Deserialize<ElementContent, ElementContentDocument>(json, document => document.ToDomain());
}
