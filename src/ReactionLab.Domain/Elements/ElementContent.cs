using ReactionLab.Domain.Common;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Domain.Elements;

public sealed class ElementContent : ITranslatableContent<ElementContent>
{
    public const int MaximumNameLength = 50;

    public static readonly Error NameRequired = Error.Validation(
        "ElementContent.NameRequired",
        "Element name is required.",
        nameof(Name));

    public static readonly Error NameTooLong = Error.Validation(
        "ElementContent.NameTooLong",
        $"Element name must not exceed {MaximumNameLength} characters.",
        nameof(Name))
        .WithArgs(("max", MaximumNameLength));

    private ElementContent(string name, string? discoveryInfo, IReadOnlyList<string> interestingFacts)
    {
        Name = name;
        DiscoveryInfo = discoveryInfo;
        InterestingFacts = interestingFacts;
    }

    public string Name { get; }

    public string? DiscoveryInfo { get; }

    public IReadOnlyList<string> InterestingFacts { get; }

    public static Result<ElementContent> Create(
        string? name,
        string? discoveryInfo = null,
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

        return new ElementContent(
            trimmed,
            TextNormalizer.Clean(discoveryInfo),
            TextNormalizer.CleanAll(interestingFacts));
    }

    public ElementContent WithFallback(ElementContent fallback) =>
        new(Name, DiscoveryInfo ?? fallback.DiscoveryInfo, InterestingFacts.Count > 0 ? InterestingFacts : fallback.InterestingFacts);
}
