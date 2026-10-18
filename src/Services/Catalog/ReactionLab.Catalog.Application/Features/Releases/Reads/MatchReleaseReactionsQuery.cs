using ReactionLab.Catalog.Application.Common.Abstractions;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed record MatchReleaseReactionsQuery(ReleaseRequest Release, IReadOnlyList<Guid> Available, ReactantMatch Match);
