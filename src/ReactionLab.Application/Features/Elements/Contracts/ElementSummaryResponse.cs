using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Elements.Contracts;

public sealed record ElementSummaryResponse(
    Guid Id,
    int AtomicNumber,
    string Symbol,
    string Name,
    decimal Mass,
    ElementCategory Category,
    int Period,
    int? Group,
    string DisplayColor);
