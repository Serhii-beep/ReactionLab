using ReactionLab.Application.Common.Pagination;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Reactions.ListReactions;

public sealed record ListReactionsQuery(
    string? Search,
    IReadOnlyCollection<Guid>? AvailableSubstanceIds,
    CursorRequest Page,
    SupportedLocale Locale);
