using System.ComponentModel.DataAnnotations;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.Contracts;

public sealed record TranslateElementRequest(
    [property: Required]
    [property: MaxLength(ElementContent.MaximumNameLength)]
    string? Name,
    string? DiscoveryInfo,
    IReadOnlyList<string>? InterestingFacts);
