using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Common.Pagination;
using ReactionLab.Catalog.Application.Features.Releases.Search;
using ReactionLab.Catalog.Application.Features.Releases.Snapshots;
using ReactionLab.Catalog.Application.Features.Substances;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed class SearchReleaseSubstancesHandler(IReleaseReadContext release, IReleaseWordSearch wordSearch, HybridCache cache)
    : IQueryHandler<SearchReleaseSubstancesQuery, CursorPagedResult<SubstanceSummaryResponse>>
{
    public const int PageSize = 30;

    private const int MaximumDepth = 1000;

    private const int MaximumPrefixNames = 1000;

    private static readonly string AfterEveryKey = char.ConvertFromUtf32(0x10FFFF);

    public async ValueTask<Result<CursorPagedResult<SubstanceSummaryResponse>>> HandleAsync(
        SearchReleaseSubstancesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Search is { } term)
        {
            if (!SearchKeys.IsNormalized(term))
            {
                return ReleaseErrors.TermNotCanonical;
            }

            if (SearchKeys.LengthOf(term) is < SearchKeys.MinimumLength or > SearchKeys.MaximumLength)
            {
                return ReleaseErrors.TermLength;
            }

            if (query.Element is not null)
            {
                return ReleaseErrors.SearchWithElement;
            }
        }

        ElementSymbol? element = null;

        if (query.Element is not null
            && ((element = ElementSymbol.Match(query.Element)) is null
                || !string.Equals(element.Value, query.Element, StringComparison.Ordinal)))
        {
            return SubstanceErrors.InvalidElement(query.Element);
        }

        var skip = 0;

        if (query.Cursor is not null)
        {
            var cursor = Cursor.Decode(query.Cursor);

            if (cursor.IsFailure
                || cursor.Value.IsKeyset
                || cursor.Value.Skip <= 0
                || cursor.Value.Skip % PageSize != 0
                || !string.Equals(cursor.Value.Encode(), query.Cursor, StringComparison.Ordinal))
            {
                return ReleaseErrors.CursorNotOffset;
            }

            skip = cursor.Value.Skip;
        }

        if (skip >= MaximumDepth)
        {
            return ReleaseErrors.TooDeep(MaximumDepth);
        }

        release.UseRelease(query.Release.ReleaseId);

        var page = await cache.GetOrCreateAsync(
            CacheKeys.ReleaseSubstances(query.Release.ReleaseId, query.Release.Locale, query.Search, element?.Value, skip),
            async token => await PageAsync(query.Search, element, query.Release.Locale, skip, token),
            CachePolicies.Release,
            cancellationToken: cancellationToken);

        return Result.Success(page);
    }

    private async Task<CursorPagedResult<SubstanceSummaryResponse>> PageAsync(
        string? term,
        ElementSymbol? element,
        SupportedLocale locale,
        int skip,
        CancellationToken cancellationToken)
    {
        var ranked = term is null
            ? await ListAsync(element, skip, cancellationToken)
            : [.. (await RankAsync(term, locale, skip + PageSize + 1, cancellationToken)).Skip(skip)];

        var page = ranked.Take(PageSize).Select(SubstanceId.From).ToList();
        var summaries = await SubstanceQueries.SummariesAsync(
            release.Substances.Where(substance => page.Contains(substance.Id)),
            locale,
            cancellationToken);
        var byId = summaries.ToDictionary(summary => summary.Id);
        var hasMore = ranked.Count > PageSize;

        return new CursorPagedResult<SubstanceSummaryResponse>
        {
            Items = [.. page.Where(id => byId.ContainsKey(id.Value)).Select(id => byId[id.Value])],
            PageSize = PageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.Skipping(skip + PageSize).Encode() : null
        };
    }

    private async Task<List<Guid>> ListAsync(ElementSymbol? element, int skip, CancellationToken cancellationToken)
    {
        if (element is not null)
        {
            return await release.SubstanceElements
                .Where(candidate => candidate.Element == element.Value)
                .OrderByDescending(candidate => candidate.Prominence)
                .ThenBy(candidate => candidate.SubstanceId)
                .Skip(skip)
                .Take(PageSize + 1)
                .Select(candidate => candidate.SubstanceId)
                .ToListAsync(cancellationToken);
        }

        var ids = await release.Substances
            .Where(substance => substance.RetiredAt == null)
            .OrderByDescending(substance => EF.Property<int>(substance, ReleaseColumns.Prominence))
            .ThenBy(substance => substance.Id)
            .Skip(skip)
            .Take(PageSize + 1)
            .Select(substance => substance.Id)
            .ToListAsync(cancellationToken);

        return ids.ConvertAll(id => id.Value);
    }

    private async Task<List<Guid>> RankAsync(string term, SupportedLocale locale, int wanted, CancellationToken cancellationToken)
    {
        var ranked = new List<Guid>();
        var seen = new HashSet<Guid>();

        void Append(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                if (seen.Add(id))
                {
                    ranked.Add(id);
                }
            }
        }

        if (SearchKeys.LengthOf(term) <= SearchKeys.MaximumPrefixLength)
        {
            Append(await release.PrefixTops
                .Where(top => top.Locale == locale.Code && top.Prefix == term)
                .OrderBy(top => top.Rank)
                .Select(top => top.SubstanceId)
                .ToListAsync(cancellationToken));

            return ranked;
        }

        var names = release.SubstanceNames.Where(name => name.Locale == locale.Code);

        Append(await Best(names.Where(name => name.NameKey == term), wanted, cancellationToken));

        if (ranked.Count < wanted && SearchKeys.Words(term).Skip(1).Any())
        {
            var upper = term + AfterEveryKey;

            Append(await Best(
                names
                    .Where(name => name.NameKey.CompareTo(term) >= 0 && name.NameKey.CompareTo(upper) < 0)
                    .OrderBy(name => name.NameKey)
                    .Take(MaximumPrefixNames),
                wanted,
                cancellationToken));
        }

        if (ranked.Count < wanted
            && SearchKeys.LongestWord(term) is { } word
            && SearchKeys.LengthOf(word) > SearchKeys.MaximumPrefixLength)
        {
            Append(await wordSearch
                .Matching(
                    release.Words.Where(candidate => candidate.Locale == locale.Code),
                    release.PrefixTops.Where(top => top.Locale == locale.Code),
                    word)
                .Take(wanted)
                .ToListAsync(cancellationToken));
        }

        return ranked;
    }

    private static async Task<List<Guid>> Best(IQueryable<ReleaseSubstanceName> names, int wanted, CancellationToken cancellationToken) =>
        await names
            .GroupBy(name => name.SubstanceId)
            .Select(group => new { SubstanceId = group.Key, Prominence = group.Max(name => name.Prominence) })
            .OrderByDescending(candidate => candidate.Prominence)
            .ThenBy(candidate => candidate.SubstanceId)
            .Take(wanted)
            .Select(candidate => candidate.SubstanceId)
            .ToListAsync(cancellationToken);
}
