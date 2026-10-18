using System.Globalization;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases;

namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed class CatalogReleaseRegistry(TimeProvider timeProvider) : ICatalogReleases
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    private RegistryState _state = RegistryState.Empty;

    public Guid InstanceId { get; } = Guid.CreateVersion7();

    public bool IsReady => Volatile.Read(ref _state).PolledAt is not null;

    public Guid? ActiveRelease => Volatile.Read(ref _state).Active;

    internal RegistryState State => Volatile.Read(ref _state);

    public ReleaseAvailability Classify(Guid releaseId)
    {
        var state = Volatile.Read(ref _state);
        var now = timeProvider.GetUtcNow();

        if (state.HeldByReleaseId.TryGetValue(releaseId, out var held) && held.IsReadableAt(now))
        {
            return ReleaseAvailability.Readable;
        }

        if (state.Known.Contains(releaseId))
        {
            return ReleaseAvailability.Superseded;
        }

        return releaseId.Version == 7
            && (state.NewestKnown is not { } newest || IsNewer(releaseId, newest))
            && CreatedAt(releaseId) <= now + ClockSkew
                ? ReleaseAvailability.Pending
                : ReleaseAvailability.Unknown;
    }

    public bool HasCurrentAnswers(Guid releaseId) =>
        Volatile.Read(ref _state).HeldByReleaseId.TryGetValue(releaseId, out var held)
        && held.AnswerVersion == CatalogAnswers.Version;

    public IReadOnlyList<MatchedReaction>? Match(
        Guid releaseId,
        IReadOnlyCollection<Guid> availableSubstanceIds,
        ReactantMatch match,
        int limit) =>
        Volatile.Read(ref _state).HeldByReleaseId.TryGetValue(releaseId, out var held)
            ? held.Index.Match(availableSubstanceIds, match, limit)
            : null;

    internal void Publish(RegistryState state) => Volatile.Write(ref _state, state);

    internal static bool IsNewer(Guid candidate, Guid known) =>
        string.CompareOrdinal(candidate.ToString("D"), known.ToString("D")) > 0;

    private static DateTimeOffset CreatedAt(Guid id) =>
        DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(id.ToString("N")[..12], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
}
