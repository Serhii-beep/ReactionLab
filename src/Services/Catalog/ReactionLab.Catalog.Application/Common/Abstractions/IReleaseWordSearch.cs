using ReactionLab.Catalog.Application.Features.Releases.Snapshots;

namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface IReleaseWordSearch
{
    IQueryable<Guid> Matching(IQueryable<ReleaseWord> words, IQueryable<ReleasePrefixTop> tops, string word);
}
