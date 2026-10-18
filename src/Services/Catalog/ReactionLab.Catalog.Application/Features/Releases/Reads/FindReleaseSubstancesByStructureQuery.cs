namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed record FindReleaseSubstancesByStructureQuery(ReleaseRequest Release, string StructureKey);
