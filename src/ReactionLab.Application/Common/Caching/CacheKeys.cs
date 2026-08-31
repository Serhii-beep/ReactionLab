using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Common.Caching;

public static class CacheKeys
{
    public static string Element(Guid id, SupportedLocale locale) =>
        $"element:{id}:{locale.Code}";

    public static string ElementBySymbol(string symbol, SupportedLocale locale) =>
        $"element:symbol:{symbol.ToUpperInvariant()}:{locale.Code}";

    public static string ElementList(string? search, SupportedLocale locale) =>
        $"elements:{Term(search)}:{locale.Code}";

    public static string Substance(Guid id, SupportedLocale locale) =>
        $"substance:{id}:{locale.Code}";

    public static string SubstanceList(string? search, string? cursor, int pageSize, SupportedLocale locale) =>
        $"substances:{Term(search)}:{cursor ?? "-"}:{pageSize}:{locale.Code}";

    public static string Reaction(Guid id, SupportedLocale locale) =>
        $"reaction:{id}:{locale.Code}";

    public const string ChemistryReference = "chemistry:reference";

    public static string ReactionList(
        string? search,
        IReadOnlyCollection<Guid>? availableSubstanceIds,
        string? cursor,
        int pageSize,
        SupportedLocale locale) =>
        $"reactions:{Term(search)}:{Available(availableSubstanceIds)}:{cursor ?? "-"}:{pageSize}:{locale.Code}";

    private static string Term(string? search) =>
        string.IsNullOrWhiteSpace(search) ? "-" : search.Trim().ToUpperInvariant();

    private static string Available(IReadOnlyCollection<Guid>? ids) =>
        ids is not { Count: > 0 } ? "-" : string.Join('.', ids.Order());
}
