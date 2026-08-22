namespace ReactionLab.Application.Features.Elements.TranslateElement;

public sealed record TranslateElementCommand(
    Guid ElementId,
    string Locale,
    string? Name,
    string? DiscoveryInfo,
    IReadOnlyList<string>? InterestingFacts);
