namespace ReactionLab.Catalog.Application.Features.Releases.Snapshots;

public sealed class ReleaseSubstanceName
{
    public Guid ReleaseId { get; init; }

    public string Locale { get; init; } = null!;

    public Guid SubstanceId { get; init; }

    public string NameKey { get; init; } = null!;

    public int Prominence { get; init; }
}
