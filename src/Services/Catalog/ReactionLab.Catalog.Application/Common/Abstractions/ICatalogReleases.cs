namespace ReactionLab.Catalog.Application.Common.Abstractions;

public interface ICatalogReleases
{
    bool IsReady { get; }

    Guid? ActiveRelease { get; }

    ReleaseAvailability Classify(Guid releaseId);

    bool HasCurrentAnswers(Guid releaseId);

    IReadOnlyList<MatchedReaction>? Match(
        Guid releaseId,
        IReadOnlyCollection<Guid> availableSubstanceIds,
        ReactantMatch match,
        int limit);
}
