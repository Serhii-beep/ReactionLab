namespace ReactionLab.Catalog.Application.Features.Releases.Snapshots;

public sealed class ReleasePrefixTop
{
    public Guid ReleaseId { get; init; }

    public string Locale { get; init; } = null!;

    public string Prefix { get; init; } = null!;

    public short Rank { get; init; }

    public Guid SubstanceId { get; init; }

    public int Prominence { get; init; }
}
