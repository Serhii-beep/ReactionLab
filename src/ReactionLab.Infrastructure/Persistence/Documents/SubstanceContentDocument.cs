using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal sealed record SubstanceContentDocument
{
    public string Name { get; init; } = string.Empty;

    public string? IupacName { get; init; }

    public string? Description { get; init; }

    public string? SafetyInformation { get; init; }

    public IReadOnlyList<string>? CommonNames { get; init; }

    public IReadOnlyList<string>? Uses { get; init; }

    public IReadOnlyList<string>? InterestingFacts { get; init; }

    public static SubstanceContentDocument FromDomain(SubstanceContent content) => new()
    {
        Name = content.Name,
        IupacName = content.IupacName,
        Description = content.Description,
        SafetyInformation = content.SafetyInformation,
        CommonNames = content.CommonNames.Count > 0 ? content.CommonNames : null,
        Uses = content.Uses.Count > 0 ? content.Uses : null,
        InterestingFacts = content.InterestingFacts.Count > 0 ? content.InterestingFacts : null
    };

    public SubstanceContent ToDomain() => PersistenceJson.Require(
        SubstanceContent.Create(
            Name, IupacName, Description, SafetyInformation, CommonNames, Uses, InterestingFacts), "substance content");

    public static string Serialize(Translations<SubstanceContent> translations) =>
        TranslationDocuments.Serialize(translations, FromDomain);

    public static Translations<SubstanceContent> Deserialize(string json) =>
        TranslationDocuments.Deserialize<SubstanceContent, SubstanceContentDocument>(json, document => document.ToDomain());
}
