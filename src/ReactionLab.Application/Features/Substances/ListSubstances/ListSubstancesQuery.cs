using ReactionLab.Application.Common.Pagination;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Substances.ListSubstances;

public sealed record ListSubstancesQuery(
    string? Search,
    CursorRequest Page,
    SupportedLocale Locale);
