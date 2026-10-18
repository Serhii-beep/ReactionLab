namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed record GetReleaseReactionQuery(ReleaseRequest Release, Guid Id);
