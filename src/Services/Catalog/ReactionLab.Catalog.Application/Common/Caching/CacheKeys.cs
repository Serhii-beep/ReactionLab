using System.Security.Cryptography;
using System.Text;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Common.Caching;

public static class CacheKeys
{
    public static string Element(string scope, Guid id, SupportedLocale locale) =>
        $"{scope}:element:{id}:{locale.Code}";

    public static string ElementBySymbol(string scope, string symbol, SupportedLocale locale) =>
        $"{scope}:element:symbol:{symbol.ToUpperInvariant()}:{locale.Code}";

    public static string ElementList(string scope, string? search, SupportedLocale locale) =>
        $"{scope}:elements:{Term(search)}:{locale.Code}";

    public static string Substance(string scope, Guid id, SupportedLocale locale) =>
        $"{scope}:substance:{id}:{locale.Code}";

    public static string SubstanceList(string scope, string? search, string? element, string? cursor, int pageSize, SupportedLocale locale) =>
        $"{scope}:substances:{Term(search)}:{element ?? "-"}:{cursor ?? "-"}:{pageSize}:{locale.Code}";

    public static string Reaction(string scope, Guid id, SupportedLocale locale) =>
        $"{scope}:reaction:{id}:{locale.Code}";

    public static string ChemistryReference(string scope) => $"{scope}:chemistry:reference";

    public static string ReactionList(
        string scope,
        string? search,
        IReadOnlyCollection<Guid>? availableSubstanceIds,
        ReactantMatch match,
        string? cursor,
        int pageSize,
        SupportedLocale locale) =>
        $"{scope}:reactions:{Term(search)}:{Bench(availableSubstanceIds)}:{match}:{cursor ?? "-"}:{pageSize}:{locale.Code}";

    public static string ReleaseSubstances(Guid release, SupportedLocale locale, string? term, string? element, int skip) =>
        $"release:{release:N}:a{CatalogAnswers.Version}:{locale.Code}:substances:{term ?? "-"}:{element ?? "-"}:{skip}";

    public static string ReleaseSubstance(Guid release, SupportedLocale locale, Guid id) =>
        $"release:{release:N}:a{CatalogAnswers.Version}:{locale.Code}:substance:{id:N}";

    public static string ReleaseStructure(Guid release, SupportedLocale locale, string structureKey) =>
        $"release:{release:N}:a{CatalogAnswers.Version}:{locale.Code}:structure:{structureKey}";

    public static string ReleaseMatch(Guid release, SupportedLocale locale, IReadOnlyCollection<Guid> available, ReactantMatch match) =>
        $"release:{release:N}:a{CatalogAnswers.Version}:{locale.Code}:match:{match}:{Bench(available)}";

    public static string ReleaseReaction(Guid release, SupportedLocale locale, Guid id) =>
        $"release:{release:N}:a{CatalogAnswers.Version}:{locale.Code}:reaction:{id:N}";

    private static string Term(string? search) =>
        string.IsNullOrWhiteSpace(search) ? "-" : search.Trim().ToUpperInvariant();

    private static string Bench(IReadOnlyCollection<Guid>? ids) =>
        ids is not { Count: > 0 }
            ? "-"
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('.', ids.Order()))))[..32];
}
