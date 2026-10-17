using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Pagination;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Reactions.ListReactions;

public sealed record ListReactionsQuery(
    string? Search,
    IReadOnlyCollection<Guid>? AvailableSubstanceIds,
    ReactantMatch Match,
    CursorRequest Page,
    SupportedLocale Locale);
