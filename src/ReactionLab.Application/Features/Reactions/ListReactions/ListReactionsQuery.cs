using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Reactions.ListReactions;

public sealed record ListReactionsQuery(
    string? Search,
    IReadOnlyCollection<Guid>? AvailableSubstanceIds,
    ReactantMatch Match,
    CursorRequest Page,
    SupportedLocale Locale);
