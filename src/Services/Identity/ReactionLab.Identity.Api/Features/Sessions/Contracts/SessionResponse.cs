namespace ReactionLab.Identity.Api.Features.Sessions.Contracts;

internal sealed record SessionResponse(
    Guid Id,
    string? Device,
    string? Network,
    DateTimeOffset SignedInAt,
    DateTimeOffset LastActiveAt,
    DateTimeOffset ExpiresAt,
    bool Current);
