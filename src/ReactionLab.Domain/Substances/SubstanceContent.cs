using ReactionLab.Domain.Common;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Domain.Substances;

public sealed class SubstanceContent : ITranslatableContent<SubstanceContent>
{
    public const int MaximumNameLength = 200;

    public const int MaximumIupacNameLength = 300;

    public static readonly Error NameRequired = Error.Validation(
        "SubstanceContent.NameRequired",
        "Substance name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "SubstanceContent.NameTooLong",
        $"Substance name must not exceed {MaximumNameLength} characters.");

    private SubstanceContent(
        string name,
        string? iupacName,
        string? description,
        string? safetyInformation,
        IReadOnlyList<string> commonNames,
        IReadOnlyList<string> uses,
        IReadOnlyList<string> interestingFacts)
    {
        Name = name;
        IupacName = iupacName;
        Description = description;
        SafetyInformation = safetyInformation;
        CommonNames = commonNames;
        Uses = uses;
        InterestingFacts = interestingFacts;
    }

    public string Name { get; }

    public string? IupacName { get; }

    public string? Description { get; }

    public string? SafetyInformation { get; }

    public IReadOnlyList<string> CommonNames { get; }

    public IReadOnlyList<string> Uses { get; }

    public IReadOnlyList<string> InterestingFacts { get; }

    public static Result<SubstanceContent> Create(
        string? name,
        string? iupacName = null,
        string? description = null,
        string? safetyInformation = null,
        IEnumerable<string>? commonNames = null,
        IEnumerable<string>? uses = null,
        IEnumerable<string>? interestingFacts = null)
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

        return new SubstanceContent(
            trimmed,
            TextNormalizer.Clean(iupacName, MaximumIupacNameLength),
            TextNormalizer.Clean(description),
            TextNormalizer.Clean(safetyInformation),
            TextNormalizer.CleanAll(commonNames),
            TextNormalizer.CleanAll(uses),
            TextNormalizer.CleanAll(interestingFacts));
    }

    public SubstanceContent WithFallback(SubstanceContent fallback) =>
        new(Name,
            IupacName ?? fallback.IupacName,
            Description ?? fallback.Description,
            SafetyInformation ?? fallback.SafetyInformation,
            CommonNames.Count > 0 ? CommonNames : fallback.CommonNames,
            Uses.Count > 0 ? Uses : fallback.Uses,
            InterestingFacts.Count > 0 ? InterestingFacts : fallback.InterestingFacts);
}
