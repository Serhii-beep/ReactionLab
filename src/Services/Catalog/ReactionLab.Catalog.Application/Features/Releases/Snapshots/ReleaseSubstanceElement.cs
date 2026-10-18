namespace ReactionLab.Catalog.Application.Features.Releases.Snapshots;

public sealed class ReleaseSubstanceElement
{
    public Guid ReleaseId { get; init; }

    public string Element { get; init; } = null!;

    public int Prominence { get; init; }

    public Guid SubstanceId { get; init; }
}
