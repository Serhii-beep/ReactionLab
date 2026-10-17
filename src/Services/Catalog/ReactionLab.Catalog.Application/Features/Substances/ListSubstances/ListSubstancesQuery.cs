using ReactionLab.Catalog.Application.Common.Pagination;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Substances.ListSubstances;

public sealed record ListSubstancesQuery(
    string? Search,
    string? Element,
    CursorRequest Page,
    SupportedLocale Locale);
