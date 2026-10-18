using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases.Snapshots;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Releases;

internal sealed class TrigramReleaseWordSearch : IReleaseWordSearch
{
    public const int MatchedWords = 50;

    private const string Escape = @"\";

    private static readonly string AfterEveryKey = char.ConvertFromUtf32(0x10FFFF);

    public IQueryable<Guid> Matching(IQueryable<ReleaseWord> words, IQueryable<ReleasePrefixTop> tops, string word)
    {
        var upper = word + AfterEveryKey;
        var pattern = $"%{EscapeWildcards(word)}%";
        var containing = words.Where(candidate => EF.Functions.Like(candidate.Word, pattern, Escape));

        var matched = words
            .Where(candidate => EF.Functions.Like(candidate.Word, pattern, Escape)
                || (!containing.Any() && EF.Functions.TrigramsAreSimilar(candidate.Word, word)))
            .Select(candidate => new
            {
                candidate.Word,
                Closeness = candidate.Word == word ? 0
                    : candidate.Word.CompareTo(word) >= 0 && candidate.Word.CompareTo(upper) < 0 ? 1
                    : EF.Functions.Like(candidate.Word, pattern, Escape) ? 2
                    : 3,
                Distance = EF.Functions.Like(candidate.Word, pattern, Escape)
                    ? 0
                    : EF.Functions.TrigramsSimilarityDistance(word, candidate.Word),
                candidate.Prominence
            })
            .OrderBy(candidate => candidate.Closeness)
            .ThenBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.Prominence)
            .ThenBy(candidate => candidate.Word)
            .Take(MatchedWords);

        return matched
            .Join(
                tops,
                candidate => candidate.Word,
                top => top.Prefix,
                (candidate, top) => new { top.SubstanceId, candidate.Closeness, candidate.Distance, top.Prominence })
            .GroupBy(entry => entry.SubstanceId)
            .Select(group => new
            {
                SubstanceId = group.Key,
                Closeness = group.Min(entry => entry.Closeness),
                Distance = group.Min(entry => entry.Distance),
                Prominence = group.Max(entry => entry.Prominence)
            })
            .OrderBy(candidate => candidate.Closeness)
            .ThenBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.Prominence)
            .ThenBy(candidate => candidate.SubstanceId)
            .Select(candidate => candidate.SubstanceId);
    }

    private static string EscapeWildcards(string term) =>
        term.Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal);
}
