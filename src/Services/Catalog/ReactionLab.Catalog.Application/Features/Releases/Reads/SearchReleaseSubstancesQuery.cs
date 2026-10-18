namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed record SearchReleaseSubstancesQuery(ReleaseRequest Release, string? Search, string? Element, string? Cursor);
