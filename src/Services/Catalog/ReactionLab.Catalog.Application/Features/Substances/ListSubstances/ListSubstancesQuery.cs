using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Catalog.Application.Features.Substances.ListSubstances;

public sealed record ListSubstancesQuery(
    string? Search,
    string? Element,
    CursorRequest Page,
    SupportedLocale Locale);
