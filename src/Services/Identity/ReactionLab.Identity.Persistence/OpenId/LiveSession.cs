namespace ReactionLab.Identity.Persistence.OpenId;

public sealed record LiveSession(
    Guid Id,
    string? Device,
    string? Address,
    DateTimeOffset SignedInAt,
    DateTimeOffset LastActiveAt,
    DateTimeOffset ExpiresAt);
