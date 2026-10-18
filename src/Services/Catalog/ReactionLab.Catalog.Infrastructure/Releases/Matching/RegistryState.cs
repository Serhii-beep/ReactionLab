namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed record RegistryState(
    Guid? Active,
    IReadOnlyDictionary<Guid, HeldRelease> HeldByReleaseId,
    IReadOnlySet<Guid> Known,
    Guid? NewestKnown,
    DateTimeOffset? PolledAt)
{
    public static RegistryState Empty { get; } = new(null, new Dictionary<Guid, HeldRelease>(), new HashSet<Guid>(), null, null);
}
