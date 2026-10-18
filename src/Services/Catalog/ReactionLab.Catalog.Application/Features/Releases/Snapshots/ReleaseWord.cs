namespace ReactionLab.Catalog.Application.Features.Releases.Snapshots;

public sealed class ReleaseWord
{
    public Guid ReleaseId { get; init; }

    public string Locale { get; init; } = null!;

    public string Word { get; init; } = null!;

    public int Prominence { get; init; }
}
