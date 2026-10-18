using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Reactions;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases;

internal static class ReleaseMatching
{
    public static async Task<IReadOnlyList<ReactionSummaryResponse>> SummariesAsync(
        IReleaseReadContext release,
        IReadOnlyList<MatchedReaction> matched,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        if (matched.Count == 0)
        {
            return [];
        }

        var ids = matched.Select(reaction => ReactionId.From(reaction.ReactionId)).ToList();
        var summaries = await ReactionQueries.SummariesAsync(
            release.Reactions.Where(reaction => ids.Contains(reaction.Id)),
            release.Substances,
            locale,
            cancellationToken);
        var byId = summaries.ToDictionary(summary => summary.Id);

        return [.. matched.Where(reaction => byId.ContainsKey(reaction.ReactionId)).Select(reaction => byId[reaction.ReactionId])];
    }
}
